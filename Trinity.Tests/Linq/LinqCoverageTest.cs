// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015-2020

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Semiodesk.Trinity.Tests.Linq
{
    /// <summary>
    /// Exhaustive LINQ→SPARQL coverage corpus using an EF-Core-style oracle: every query runs both
    /// through the SPARQL provider (<c>Model.AsQueryable&lt;T&gt;</c>) and through LINQ-to-Objects over
    /// the same seeded resources, and the results are compared. Sequence comparisons are order-
    /// insensitive by default (SPARQL has no implicit order); ordered cases project a deterministic
    /// sort key and compare in order.
    ///
    /// This corpus is green and part of the default suite, so it gates CI. It is also the regression
    /// net for the dotNetRDF 3.x upgrade — the provider emits SPARQL strings, so the same cases must
    /// keep passing there. Run it alone with:
    ///   dotnet test --filter "FullyQualifiedName~LinqCoverageTest"
    /// </summary>
    [TestFixture]
    public class LinqCoverageTest
    {
        private IStore _store;
        private IModel _model;
        private List<Person> _people;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            OntologyDiscovery.AddAssembly(typeof(Person).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(Person).Assembly);
            OntologyDiscovery.AddAssembly(typeof(Resource).Assembly);
            MappingDiscovery.RegisterAssembly(typeof(Resource).Assembly);

            _store = StoreFactory.CreateStore("provider=dotnetrdf");
            _model = _store.CreateModel(new Uri("http://example.org/coverage"));
            _model.Clear();

            Group spiders = _model.CreateResource<Group>(new Uri("http://example.org/g/spiders"));
            spiders.Name = "Spiders";
            spiders.Commit();

            Group keys = _model.CreateResource<Group>(new Uri("http://example.org/g/keys"));
            keys.Name = "Keys";
            keys.Commit();

            // A bare Agent (typed foaf:Agent, not foaf:Person).
            Agent john = _model.CreateResource<Agent>(new Uri("http://example.org/p/john"));
            john.FirstName = "John";
            john.Commit();

            Person alice = MakePerson("alice", "Alice", "Anderson", 30, 100.5f, true, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), spiders);
            Person bob = MakePerson("bob", "bob", "Brown", 30, 200f, false, new DateTime(1985, 6, 15, 0, 0, 0, DateTimeKind.Utc), spiders);
            Person carol = MakePerson("carol", "Carol", "Cook", 45, 50f, true, new DateTime(1970, 3, 20, 0, 0, 0, DateTimeKind.Utc), keys);
            // Dave: Age / AccountBalance / Status / Birthday intentionally unset (unbound → default).
            Person dave = _model.CreateResource<Person>(new Uri("http://example.org/p/dave"));
            dave.FirstName = "Dave";
            dave.LastName = "Anderson";
            dave.Group = spiders;
            dave.Commit();
            Person eve = MakePerson("eve", "Eve", "Evans", 45, 300f, true, new DateTime(2000, 12, 31, 0, 0, 0, DateTimeKind.Utc), keys);

            alice.KnownPeople.Add(bob);
            alice.KnownPeople.Add(carol);
            alice.Commit();
            bob.KnownPeople.Add(alice);
            bob.Commit();
            dave.KnownPeople.Add(alice);
            dave.KnownPeople.Add(bob);
            dave.KnownPeople.Add(carol);
            dave.Commit();
            eve.KnownPeople.Add(eve);
            eve.Commit();

            _people = new List<Person> { alice, bob, carol, dave, eve };
            _staticSpiders = spiders;
        }

        private Person MakePerson(string id, string first, string last, int age, float balance, bool status, DateTime birthday, Group group)
        {
            Person p = _model.CreateResource<Person>(new Uri("http://example.org/p/" + id));
            p.FirstName = first;
            p.LastName = last;
            p.Age = age;
            p.AccountBalance = balance;
            p.Status = status;
            p.Birthday = birthday;
            p.Group = group;
            p.Commit();
            return p;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => _store?.Dispose();

        // ---- Oracle ---------------------------------------------------------------------------

        /// <summary>A single query shape, run identically against both providers.</summary>
        public sealed class Case
        {
            public string Name;
            internal Func<IQueryable<Person>, object> Run;

            /// <summary>
            /// Optional in-memory expectation, used instead of <see cref="Run"/> on the oracle side when
            /// RDF and LINQ-to-Objects legitimately differ. Currently only needed for string ordering:
            /// SPARQL <c>ORDER BY</c> compares plain literals by Unicode codepoint (so "Zoe" precedes
            /// "alice"), whereas .NET's default comparer is culture-sensitive and case-insensitive-ish.
            /// Those cases order with <see cref="StringComparer.Ordinal"/> here.
            /// </summary>
            internal Func<IQueryable<Person>, object> Oracle;

            public bool Ordered;
            public override string ToString() => Name;
        }

        private static Case C(string name, Func<IQueryable<Person>, object> run, bool ordered = false,
                              Func<IQueryable<Person>, object> oracle = null)
            => new Case { Name = name, Run = run, Ordered = ordered, Oracle = oracle };

        [TestCaseSource(nameof(Cases))]
        public void Query(Case c)
        {
            object expected = (c.Oracle ?? c.Run)(_people.AsQueryable()); // LINQ-to-Objects oracle
            object actual = c.Run(_model.AsQueryable<Person>()); // SPARQL provider

            AssertEquivalent(expected, actual, c.Ordered);
        }

        private static void AssertEquivalent(object expected, object actual, bool ordered)
        {
            if (expected is IEnumerable ee && !(expected is string))
            {
                List<object> exp = ee.Cast<object>().Select(Normalize).ToList();
                List<object> act = ((IEnumerable)actual).Cast<object>().Select(Normalize).ToList();

                if (ordered)
                {
                    CollectionAssert.AreEqual(exp, act);
                }
                else
                {
                    CollectionAssert.AreEquivalent(exp, act);
                }
            }
            else
            {
                Assert.AreEqual(expected, actual);
            }
        }

        private static object Normalize(object o) => o is Resource r ? (object)r.Uri.ToString() : o;

        // ---- Corpus ---------------------------------------------------------------------------

        public static IEnumerable<Case> Cases()
        {
            // A. Where — comparison operators per datatype
            yield return C("where int ==", q => q.Where(p => p.Age == 30).ToList());
            yield return C("where int !=", q => q.Where(p => p.Age != 30).ToList());
            yield return C("where int <", q => q.Where(p => p.Age < 45).ToList());
            yield return C("where int <=", q => q.Where(p => p.Age <= 30).ToList());
            yield return C("where int >", q => q.Where(p => p.Age > 30).ToList());
            yield return C("where int >=", q => q.Where(p => p.Age >= 45).ToList());
            yield return C("where float ==", q => q.Where(p => p.AccountBalance == 200f).ToList());
            yield return C("where float >", q => q.Where(p => p.AccountBalance > 100f).ToList());
            yield return C("where float <=", q => q.Where(p => p.AccountBalance <= 100.5f).ToList());
            yield return C("where bool ==true", q => q.Where(p => p.Status == true).ToList());
            yield return C("where bool member", q => q.Where(p => p.Status).ToList());
            yield return C("where bool !member", q => q.Where(p => !p.Status).ToList());
            yield return C("where string ==", q => q.Where(p => p.FirstName == "Alice").ToList());
            yield return C("where string !=", q => q.Where(p => p.FirstName != "Alice").ToList());
            yield return C("where datetime ==", q => q.Where(p => p.Birthday == new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToList());
            yield return C("where datetime <", q => q.Where(p => p.Birthday < new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToList());
            yield return C("where datetime >=", q => q.Where(p => p.Birthday >= new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToList());
            yield return C("where resource ==", q => q.Where(p => p.Group == _staticSpiders).ToList());

            // B. Boolean composition
            yield return C("where &&", q => q.Where(p => p.Age >= 30 && p.Status).ToList());
            yield return C("where ||", q => q.Where(p => p.Age < 30 || p.FirstName == "Eve").ToList());
            yield return C("where !( ==)", q => q.Where(p => !(p.FirstName == "Alice")).ToList());
            yield return C("where nested (a||b)&&c", q => q.Where(p => (p.FirstName == "Alice" || p.FirstName == "Eve") && p.Status).ToList());
            yield return C("where !(a&&b)", q => q.Where(p => !(p.Age == 30 && p.Status)).ToList());

            // C. String methods
            yield return C("where Contains", q => q.Where(p => p.FirstName.Contains("li")).ToList());
            yield return C("where StartsWith", q => q.Where(p => p.FirstName.StartsWith("A")).ToList());
            yield return C("where EndsWith", q => q.Where(p => p.LastName.EndsWith("son")).ToList());
            yield return C("where ToLower ==", q => q.Where(p => p.FirstName.ToLower() == "alice").ToList());
            yield return C("where ToUpper ==", q => q.Where(p => p.FirstName.ToUpper() == "BOB").ToList());
            yield return C("where Equals", q => q.Where(p => p.FirstName.Equals("Carol")).ToList());
            yield return C("where Length ==", q => q.Where(p => p.FirstName.Length == 3).ToList());
            yield return C("where Length >", q => q.Where(p => p.LastName.Length > 5).ToList());
            yield return C("where Regex.IsMatch", q => q.Where(p => Regex.IsMatch(p.FirstName, "^A")).ToList());

            // D. Nested / member-to-member access
            yield return C("where nested member ==", q => q.Where(p => p.Group.Name == "Spiders").ToList());
            yield return C("where nested member Contains", q => q.Where(p => p.Group.Name.Contains("pi")).ToList());

            // E. Contains on in-memory collection
            yield return C("where int[] Contains", q => { var ages = new[] { 30, 45 }; return q.Where(p => ages.Contains(p.Age)).ToList(); });
            yield return C("where string[] Contains", q => { var names = new[] { "Alice", "Eve" }; return q.Where(p => names.Contains(p.FirstName)).ToList(); });
            yield return C("where empty[] Contains", q => { var none = new int[0]; return q.Where(p => none.Contains(p.Age)).ToList(); });

            // F. Unbound / default-value members
            yield return C("where unbound int == 0", q => q.Where(p => p.Age == 0).ToList());
            yield return C("where unbound int != 0", q => q.Where(p => p.Age != 0).ToList());
            yield return C("where unbound float == 0", q => q.Where(p => p.AccountBalance == 0f).ToList());
            yield return C("where unbound bool == false", q => q.Where(p => p.Status == false).ToList());
            yield return C("select unbound int", q => q.Select(p => p.Age).ToList());

            // G. Ordering (compare deterministic projected key sequences)
            yield return C("orderby int asc", q => q.OrderBy(p => p.Age).ThenBy(p => p.FirstName).Select(p => p.FirstName).ToList(), ordered: true);
            yield return C("orderby int desc", q => q.OrderByDescending(p => p.Age).ThenBy(p => p.FirstName).Select(p => p.FirstName).ToList(), ordered: true);
            yield return C("orderby string asc", q => q.OrderBy(p => p.FirstName).Select(p => p.FirstName).ToList(), ordered: true,
                oracle: q => q.AsEnumerable().OrderBy(p => p.FirstName, StringComparer.Ordinal).Select(p => p.FirstName).ToList());
            yield return C("orderby string desc", q => q.OrderByDescending(p => p.FirstName).Select(p => p.FirstName).ToList(), ordered: true,
                oracle: q => q.AsEnumerable().OrderByDescending(p => p.FirstName, StringComparer.Ordinal).Select(p => p.FirstName).ToList());
            yield return C("orderby float", q => q.OrderBy(p => p.AccountBalance).Select(p => p.FirstName).ToList(), ordered: true);
            yield return C("orderby datetime", q => q.OrderBy(p => p.Birthday).Select(p => p.FirstName).ToList(), ordered: true);
            yield return C("orderby then desc", q => q.OrderBy(p => p.Status).ThenByDescending(p => p.Age).ThenBy(p => p.FirstName).Select(p => p.FirstName).ToList(), ordered: true);

            // H. Paging (deterministic order + unique key projection)
            yield return C("skip", q => q.OrderBy(p => p.FirstName).Skip(2).Select(p => p.FirstName).ToList(), ordered: true,
                oracle: q => q.AsEnumerable().OrderBy(p => p.FirstName, StringComparer.Ordinal).Skip(2).Select(p => p.FirstName).ToList());
            yield return C("take", q => q.OrderBy(p => p.FirstName).Take(2).Select(p => p.FirstName).ToList(), ordered: true,
                oracle: q => q.AsEnumerable().OrderBy(p => p.FirstName, StringComparer.Ordinal).Take(2).Select(p => p.FirstName).ToList());
            yield return C("skip+take", q => q.OrderBy(p => p.FirstName).Skip(1).Take(2).Select(p => p.FirstName).ToList(), ordered: true,
                oracle: q => q.AsEnumerable().OrderBy(p => p.FirstName, StringComparer.Ordinal).Skip(1).Take(2).Select(p => p.FirstName).ToList());
            yield return C("where+orderby+skip+take", q => q.Where(p => p.Age >= 30).OrderBy(p => p.FirstName).Skip(1).Take(2).Select(p => p.FirstName).ToList(), ordered: true,
                oracle: q => q.Where(p => p.Age >= 30).AsEnumerable().OrderBy(p => p.FirstName, StringComparer.Ordinal).Skip(1).Take(2).Select(p => p.FirstName).ToList());

            // I. Distinct
            yield return C("distinct ages", q => q.Select(p => p.Age).Distinct().ToList());
            yield return C("distinct lastname", q => q.Select(p => p.LastName).Distinct().ToList());
            yield return C("distinct resources", q => q.Distinct().ToList());

            // J. Quantifiers / element operators
            yield return C("count", q => q.Count());
            yield return C("count(pred)", q => q.Count(p => p.Age == 45));
            yield return C("longcount", q => q.LongCount());
            yield return C("any", q => q.Any());
            yield return C("any(pred true)", q => q.Any(p => p.FirstName == "Alice"));
            yield return C("any(pred false)", q => q.Any(p => p.FirstName == "Zzz"));
            yield return C("all true", q => q.All(p => p.Age >= 0));
            yield return C("all false", q => q.All(p => p.Status));
            yield return C("first(pred)", q => q.Where(p => p.FirstName == "Alice").First().FirstName);
            yield return C("firstordefault none", q => q.Where(p => p.FirstName == "Zzz").Select(p => p.FirstName).FirstOrDefault());
            yield return C("single(pred)", q => q.Where(p => p.FirstName == "Carol").Single().FirstName);
            yield return C("ordered first", q => q.OrderBy(p => p.FirstName).First().FirstName);
            yield return C("ordered last", q => q.OrderBy(p => p.FirstName).Last().FirstName,
                oracle: q => q.AsEnumerable().OrderBy(p => p.FirstName, StringComparer.Ordinal).Last().FirstName);

            // K. Aggregates
            yield return C("sum ages", q => q.Select(p => p.Age).Sum());
            yield return C("min age", q => q.Select(p => p.Age).Min());
            yield return C("max age", q => q.Select(p => p.Age).Max());
            yield return C("average age", q => q.Select(p => p.Age).Average());
            yield return C("max balance", q => q.Select(p => p.AccountBalance).Max());

            // L. GroupBy
            yield return C("groupby key", q => q.GroupBy(p => p.Status).Select(g => g.Key).ToList());
            yield return C("groupby count", q => q.GroupBy(p => p.Age).Select(g => g.Count()).ToList());
            yield return C("groupby key+count", q => q.GroupBy(p => p.Status).Select(g => g.Key.ToString() + ":" + g.Count()).ToList());

            // M. Set operations
            yield return C("union", q => q.Where(p => p.Age == 30).Union(q.Where(p => p.Age == 45)).ToList());
            yield return C("concat", q => q.Where(p => p.Age == 30).Concat(q.Where(p => p.FirstName == "Alice")).Select(p => p.FirstName).ToList());
            yield return C("intersect", q => q.Where(p => p.Status).Intersect(q.Where(p => p.Age == 45)).ToList());
            yield return C("except", q => q.Where(p => p.Age == 45).Except(q.Where(p => p.Status)).ToList());

            // N. Projections
            yield return C("select member string", q => q.Select(p => p.FirstName).ToList());
            yield return C("select member int", q => q.Where(p => p.Age > 0).Select(p => p.Age).ToList());
            yield return C("select resource member", q => q.Select(p => p.Group).ToList());
            yield return C("select computed", q => q.Select(p => p.FirstName + "!").ToList());
            yield return C("select nested member", q => q.Select(p => p.Group.Name).ToList());
            yield return C("select anonymous", q => q.Where(p => p.Age == 45).Select(p => new { p.FirstName, p.Age }).ToList());
            yield return C("selectmany collection", q => q.SelectMany(p => p.KnownPeople).Select(k => k.FirstName).ToList());

            // N2. Runtime type checks (`is`, GetType() == typeof(T)) and OfType over a collection
            yield return C("where is mapped type", q => q.Where(p => p.Group is Group).ToList());
            yield return C("where GetType()==typeof(self)", q => q.Where(p => p.GetType() == typeof(Person)).ToList());
            yield return C("where member GetType()==typeof", q => q.Where(p => p.Group.GetType() == typeof(Group)).ToList());
            yield return C("where member GetType()!=typeof", q => q.Where(p => p.Group.GetType() != typeof(Person)).ToList());
            yield return C("where collection OfType Count", q => q.Where(p => p.KnownPeople.OfType<Person>().Count() > 1).ToList());

            // N3. SelectMany with a result selector (the `from ... from ...` query form)
            yield return C("selectmany result selector, duplicates",
                q => q.SelectMany(p => p.KnownPeople, (p, k) => p).Select(p => p.FirstName).ToList());
            yield return C("selectmany transparent identifier",
                q => (from p in q from k in p.KnownPeople where k.FirstName == "Alice" select p).Select(p => p.FirstName).ToList());
            yield return C("selectmany element result selector",
                q => q.SelectMany(p => p.KnownPeople, (p, k) => k).Select(k => k.FirstName).ToList());

            // O. Subqueries over collections
            yield return C("where collection Any", q => q.Where(p => p.KnownPeople.Any()).ToList());
            yield return C("where collection Count>1", q => q.Where(p => p.KnownPeople.Count > 1).ToList());
            yield return C("where collection Any(pred)", q => q.Where(p => p.KnownPeople.Any(k => k.FirstName == "Alice")).ToList());

            // P. Combinations
            yield return C("where.select.distinct", q => q.Where(p => p.Age >= 30).Select(p => p.LastName).Distinct().ToList());
            yield return C("where.count", q => q.Where(p => p.Status).Count());
            yield return C("where.orderby.first", q => q.Where(p => p.Age == 45).OrderBy(p => p.FirstName).First().FirstName);
            yield return C("where.any", q => q.Where(p => p.Age == 45).Any(p => p.Status));
        }

        // Set in OneTimeSetUp so the resource-equality case can reference a store resource.
        private static Group _staticSpiders;

        // ---- Dedicated cases where the LINQ-to-Objects oracle is not valid --------------------

        [Test]
        public void OfType_matches_only_store_typed_resources()
        {
            // Persons are foaf:Person, not foaf:Agent, in the store — so an Agent query returns only John.
            var agents = _model.AsQueryable<Agent>().ToList();
            Assert.AreEqual(1, agents.Count);
            Assert.AreEqual("John", agents[0].FirstName);
        }
    }
}

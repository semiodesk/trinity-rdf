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
// Copyright (c) Semiodesk GmbH 2026

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A read-only view over three named graphs with working-copy semantics: a
    /// <c>baseline</c> graph plus a pending change held as an <c>additions</c> and a
    /// <c>removals</c> graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads through the view see <c>(baseline − removals) ∪ additions</c> — the baseline as
    /// if the pending change had been applied, deletions included — while the baseline itself
    /// stays untouched until the change is accepted or discarded. It is a git working tree
    /// expressed in RDF.
    /// </para>
    /// <para>
    /// A triple present in both <see cref="Additions"/> and <see cref="Removals"/> is
    /// <b>visible</b>: additions win. Staging an addition always makes it readable, which is
    /// what makes an edit (remove the old value, add the new one) behave the way callers expect.
    /// </para>
    /// <para>
    /// This is deliberately <b>not</b> an <see cref="IModelGroup"/>. A group is an
    /// <c>ISet&lt;IModel&gt;</c> whose members are interchangeable and merely unioned; the three
    /// graphs here have distinct roles, so set membership has no meaning for them. Keeping the
    /// types apart also stops the existing <c>is IModelGroup</c> code paths — which can only
    /// express union — from mistaking a layered view for one.
    /// </para>
    /// <para>
    /// The view is <b>read-only</b> in this release: every mutating member of
    /// <see cref="IModel"/> throws <see cref="System.NotSupportedException"/>, exactly as
    /// <c>ModelGroup</c> does. Stage a change by writing to <see cref="Additions"/> and
    /// <see cref="Removals"/>, which are ordinary models.
    /// </para>
    /// <para>
    /// Only the reads Trinity itself formulates can honour the overlay, because subtraction has
    /// to live inside the graph pattern rather than the dataset clause. Caller-supplied SPARQL
    /// and inference-enabled reads therefore <b>throw</b> rather than quietly returning
    /// unsubtracted triples. See <c>doc/adr/0041-layered-read-views.md</c>.
    /// </para>
    /// </remarks>
    public interface ILayeredModel : IModel
    {
        /// <summary>
        /// The unchanged graph the view reads through. Never written to by the view.
        /// </summary>
        IModel Baseline { get; }

        /// <summary>
        /// Triples staged for addition. Visible through the view, and they win over
        /// <see cref="Removals"/>.
        /// </summary>
        IModel Additions { get; }

        /// <summary>
        /// Triples staged for removal. Invisible through the view while remaining present in
        /// <see cref="Baseline"/>.
        /// </summary>
        IModel Removals { get; }

        /// <summary>
        /// Applies the staged change to <see cref="Baseline"/> and empties both layers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Deliberately not named <c>Commit</c>. A view owns staging, so
        /// <see cref="Resource.Commit"/> on a resource read through one already means "stage"; reusing
        /// the word here for "push everything to the baseline" would invite exactly the confusion the
        /// two operations need to avoid.
        /// </para>
        /// <para>
        /// Removals are applied before additions, so a triple in both survives — the same precedence
        /// the read path gives it.
        /// </para>
        /// </remarks>
        /// <param name="force">Apply even if the baseline diverged. See <see cref="HasDiverged"/>.</param>
        /// <param name="transaction">
        /// Transaction to run in; one is started when omitted.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when the baseline diverged and <paramref name="force"/> is not set.
        /// </exception>
        void Accept(bool force = false, ITransaction transaction = null);

        /// <summary>
        /// Abandons the staged change, leaving <see cref="Baseline"/> untouched.
        /// </summary>
        void Discard(ITransaction transaction = null);

        /// <summary>
        /// Indicates whether the baseline has moved on ground the staged change depends on — a triple
        /// staged for removal that the baseline no longer holds.
        /// </summary>
        /// <remarks>
        /// This matters because applying a stale change never fails on its own: a changeset is a set of
        /// triples and <c>INSERT</c>/<c>DELETE</c> are idempotent, so a competing edit to a
        /// single-valued property silently leaves two values behind rather than raising anything. The
        /// check is O(removals) and deliberately conservative — it also reports the benign case where
        /// someone else already made the same removal.
        /// </remarks>
        bool HasDiverged();
    }
}

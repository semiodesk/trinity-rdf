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
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Store
{

    /// <summary>
    /// Virtuoso transactions support 
    /// For more information refer to : 
    /// http://docs.openlinksw.com/virtuoso/coredbengine.html#Locking
    /// http://docs.openlinksw.com/virtuoso/ptune.html#TRANSACTION_ISOLATION_LEVELS
    /// Auto row commit = DEFINE sql:log-enable 2
    /// </summary>
    public class VirtuosoTransaction : ITransaction
    {
        public OpenLink.Data.Virtuoso.VirtuosoTransaction Transaction;
        //IModel Model;
        VirtuosoStore RdfStore;

        bool Disposed = false;

        internal VirtuosoTransaction(VirtuosoStore rdfStore)
        {
            //Model = model;
            RdfStore = rdfStore;

        }

        public System.Data.IsolationLevel IsolationLevel
        {
            get
            {
                return Transaction.IsolationLevel;
            }

        }

        public void Commit()
        {
            Transaction.Commit();

            if (OnFinishedTransaction != null)
                OnFinishedTransaction(this, new TransactionEventArgs(true));

            this.Dispose();
        }

        public void Rollback()
        {
            Transaction.Rollback();

            if (OnFinishedTransaction != null)
                OnFinishedTransaction(this, new TransactionEventArgs(false));

            this.Dispose();
        }

        //public IModel GetModel()
        //{
        //    return Model;
        //}

        private IStore GetRdfStore()
        {
            return RdfStore;
        }

        public void Dispose()
        {
            if (Disposed)
                return;

            Transaction.Dispose();
            if (OnFinishedTransaction != null)
            {
                foreach (Delegate e in OnFinishedTransaction.GetInvocationList())
                {
                    OnFinishedTransaction -= (FinishedTransaction)e;
                }
            }

            Disposed = true;
        }

        public event FinishedTransaction OnFinishedTransaction;
    }

}

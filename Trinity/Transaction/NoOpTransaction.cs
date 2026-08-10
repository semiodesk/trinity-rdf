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
// Copyright (c) Semiodesk GmbH 2023

using System.Data;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A transaction handle for stores that do not support transactions. It satisfies the
    /// <see cref="ITransaction"/> contract and raises the usual events, but neither isolates nor rolls
    /// back anything: writes made inside it are applied immediately and <see cref="Rollback"/> cannot
    /// undo them.
    /// </summary>
    /// <remarks>
    /// This exists so <see cref="IStore.BeginTransaction(IsolationLevel)"/> never returns <c>null</c>.
    /// Returning null forced every caller to null-check, and — worse — made transactional behaviour
    /// untestable in the fast in-memory fixture, where a guard would pass while meaning nothing.
    ///
    /// <see cref="IsolationLevel"/> reports <see cref="System.Data.IsolationLevel.Unspecified"/>, which
    /// is the honest answer: callers that genuinely require isolation should check it rather than assume
    /// a non-null handle implies transactional guarantees.
    /// </remarks>
    public class NoOpTransaction : ITransaction
    {
        #region Members

        /// <summary>
        /// Always <see cref="System.Data.IsolationLevel.Unspecified"/> — nothing is isolated.
        /// </summary>
        public IsolationLevel IsolationLevel => IsolationLevel.Unspecified;

        /// <summary>
        /// Raised when the transaction is committed or rolled back.
        /// </summary>
        public event FinishedTransactionEvent OnFinishedTransaction;

        #endregion

        #region Methods

        /// <summary>
        /// Completes the transaction. The writes were already applied, so this only signals completion.
        /// </summary>
        public void Commit()
        {
            OnFinishedTransaction?.Invoke(this, new TransactionEventArgs(true));

            Dispose();
        }

        /// <summary>
        /// Signals the transaction as failed. Writes already made are <b>not</b> undone — the underlying
        /// store has no mechanism to undo them.
        /// </summary>
        public void Rollback()
        {
            OnFinishedTransaction?.Invoke(this, new TransactionEventArgs(false));

            Dispose();
        }

        /// <summary>
        /// Releases the transaction. Nothing is held, so this does nothing.
        /// </summary>
        public void Dispose()
        {
        }

        #endregion
    }
}

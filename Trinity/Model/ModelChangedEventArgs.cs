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

namespace Semiodesk.Trinity
{
    /// <summary>
    /// Interface for ModelChanged event handlers.
    /// </summary>
    /// <param name="sender">The object which raised the event.</param>
    /// <param name="e">Event specific arguments.</param>
    public delegate void ModelChangedDelegate(object sender, ModelChangedEventArgs e);

    /// <summary>
    /// Lists all possible changes to a RDF model.
    /// </summary>
    public enum ModelChangeType { Created, Deleted, Updated };

    /// <summary>
    /// Provides information about changes in a RDF model.
    /// </summary>
    public class ModelChangedEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// The kind of change to the model.
        /// </summary>
        public readonly ModelChangeType ChangeType;

        /// <summary>
        /// Resources which are subject to the change.
        /// </summary>
        public readonly IEnumerable<IResource> ChangedResources;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new ModelChangedEventArgs instance.
        /// </summary>
        /// <param name="changeType">The kind of change to the model.</param>
        /// <param name="changedResources">Resources which are subject to the change.</param>
        public ModelChangedEventArgs(ModelChangeType changeType, IEnumerable<IResource> changedResources) : base()
        {
            ChangeType = changeType;
            ChangedResources = changedResources;
        }

        #endregion
    }
}

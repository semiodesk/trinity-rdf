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
// Copyright (c) Semiodesk GmbH 2015-2019

namespace Semiodesk.Trinity.CilGenerator.Tasks
{
    /// <summary>
    /// A task for generating MSIL byte code.
    /// </summary>
    public interface IGeneratorTask
    {
        #region Methods

        /// <summary>
        /// Indicates if the task can be executed.
        /// </summary>
        /// <param name="parameter">A task specific parameter.</param>
        /// <returns><c>true</c> if the task can be executed.</returns>
        bool CanExecute(object parameter = null);

        /// <summary>
        /// Execute the task.
        /// </summary>
        /// <param name="parameter">A task specific parameter.</param>
        /// <returns><c>true</c> if the task has modified the assembly, <c>false</c> if not.</returns>
        bool Execute(object parameter = null);

        #endregion
    }
}

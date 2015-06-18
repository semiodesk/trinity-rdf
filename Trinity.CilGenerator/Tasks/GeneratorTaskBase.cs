/* Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 * Copyright (c) Semiodesk GmbH 2015
 * 
 * AUTHORS
 * - Moritz Eberl <moritz@semiodesk.com>
 * - Sebastian Faubel <sebastian@semiodesk.com>
 */

using Mono.Cecil;

namespace Semiodesk.Trinity.CilGenerator.Tasks
{
  public class GeneratorTaskBase : IGeneratorTask
  {
    #region Members

    protected AssemblyDefinition Assembly
    {
      get { return Generator.Assembly; }
    }

    protected ILGenerator Generator { get; private set; }

    protected ILogger Log
    {
      get { return Generator.Log; }
    }

    protected ModuleDefinition MainModule
    {
      get { return Generator.Assembly.MainModule; }
    }

    protected TypeDefinition Type { get; private set; }

    #endregion

    #region Constructors

    public GeneratorTaskBase(ILGenerator generator, TypeDefinition type)
    {
      Generator = generator;
      Type = type;
    }

    #endregion

    #region Methods

    public virtual bool CanExecute(object parameter = null)
    {
      return false;
    }

    public virtual bool Execute(object parameter = null)
    {
      return false;
    }

    #endregion
  }
}

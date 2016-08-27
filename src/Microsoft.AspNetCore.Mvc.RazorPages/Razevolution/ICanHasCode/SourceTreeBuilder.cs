using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Razevolution.ICanHasCode
{
    public class SourceTreeBuilder
    {
        private readonly Stack<CSharpBlock> _activeScopes;
        private readonly Action _endBlock;

        public SourceTreeBuilder()
        {
            Root = new CSharpBlock();
            _activeScopes = new Stack<CSharpBlock>();
            _activeScopes.Push(Root);

            _endBlock = EndBlock;
        }

        public CSharpBlock Root { get; }

        private CSharpBlock CurrentScope => _activeScopes.Peek();

        public IDisposable BuildBlock<TBlock>() where TBlock : CSharpBlock, new()
        {
            return BuildBlock<TBlock>(configure: null);
        }

        public IDisposable BuildBlock<TBlock>(Action<TBlock> configure) where TBlock : CSharpBlock, new()
        {
            var csharpBlock = new TBlock();

            configure?.Invoke(csharpBlock);

            Add(csharpBlock);

            _activeScopes.Push(csharpBlock);

            var builderScope = new BlockBuililerScope(_endBlock);
            return builderScope;
        }

        private void EndBlock()
        {
            var poppedBlock = _activeScopes.Pop();
        }

        public void Add(ICSharpSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            CurrentScope.Children.Add(source);
        }

        private class BlockBuililerScope : IDisposable
        {
            private readonly Action _onDispose;

            public BlockBuililerScope(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose();
            }
        }
    }
}

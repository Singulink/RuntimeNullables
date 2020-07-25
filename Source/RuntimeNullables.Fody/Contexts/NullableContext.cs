using System;
using Mono.Cecil;
using RuntimeNullables.Fody.Extensions;

namespace RuntimeNullables.Fody.Contexts
{
    internal abstract class NullableContext
    {
        private readonly bool _isNullable;
        private readonly bool _nullChecksEnabled;

        private bool _buildCalled;

        /// <summary>
        /// Initializes a new instance of the <see cref="NullableContext"/> class. This constructor should only be used for root contexts with no parent
        /// contexts.
        /// </summary>
        internal NullableContext(ICustomAttributeProvider attributeProvider, WeavingContext weavingContext)
        {
            WeavingContext = weavingContext;

            _isNullable = false;
            _nullChecksEnabled = true;

            ProcessAttributes(attributeProvider, ref _isNullable, ref _nullChecksEnabled);
        }

        internal NullableContext(ICustomAttributeProvider attributeProvider, NullableContext parentContext)
        {
            WeavingContext = parentContext.WeavingContext;

            _isNullable = parentContext._isNullable;
            _nullChecksEnabled = parentContext._nullChecksEnabled;

            ProcessAttributes(attributeProvider, ref _isNullable, ref _nullChecksEnabled);
        }

        /// <summary>
        /// Gets the current weaving context.
        /// </summary>
        public WeavingContext WeavingContext { get; }

        /// <summary>
        /// Gets a value indicating whether the current context is nullable by default.
        /// </summary>
        public bool IsNullable => _isNullable;

        /// <summary>
        /// Gets a value indicating whether the current context has null checks enabled.
        /// </summary>
        public bool NullChecksEnabled => _nullChecksEnabled;

        /// <summary>
        /// Builds the context data.
        /// </summary>
        public virtual void Build()
        {
            if (_buildCalled)
                throw new InvalidOperationException("Build method has already been called on this context.");

            _buildCalled = true;
        }

        /// <summary>
        /// Determines the nullability of an item that belongs to this type.
        /// </summary>
        protected bool IsContextItemNullable(ICustomAttributeProvider item) => IsContextInnerItemNullable(item, -1);

        protected bool IsContextInnerItemNullable(ICustomAttributeProvider item, int innerOrdinal)
        {
            var nullable = item.GetNullableAttributeValue(innerOrdinal + 1, WeavingContext);
            return nullable == null ? IsNullable : nullable.GetValueOrDefault().ToIsNullable(WeavingContext);
        }

        private void ProcessAttributes(ICustomAttributeProvider item, ref bool nullable, ref bool nullChecksEnabled)
        {
            if (item.GetNullChecksAttributeValue(WeavingContext, true) is bool nullChecksValue)
                nullChecksEnabled = nullChecksValue;

            if (item.GetNullableContextAttributeValue(WeavingContext) is NullableValue nullableValue)
                nullable = nullableValue.ToIsNullable(WeavingContext);
        }
    }
}

using System.Runtime.CompilerServices;

namespace SimpleTerrain {

	public sealed class Pool<T> {

		private readonly System.Func<T> _createObject;

		public readonly System.Collections.Generic.Stack<T> objects = new();

		public Pool(System.Func<T> createObject, int initialCount) {
			_createObject = createObject;

			for (int i = 0; i < initialCount; i++) {
				objects.Push(createObject.Invoke());
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Get() {
			return objects.Count > 0 ? objects.Pop() : _createObject.Invoke();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Return(T item) {
			objects.Push(item);
		}
	}
}

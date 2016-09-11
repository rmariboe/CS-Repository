namespace System.Collections { 
 using System;
 using System.Security.Permissions; 
 using System.Diagnostics; 
 
 [DebuggerTypeProxy(typeof(System.Collections.Ring.RingDebugView))]
 [DebuggerDisplay("Count = {Count}")]
 [System.Runtime.InteropServices.ComVisible(true)] 
 [Serializable()] public class Ring : ICollection, ICloneable {
  private Object[] _array;
  private int _head;    // First valid element in the queue 
  private int _tail;    // Last valid element in the queue
  private int _size;    // Number of elements. 
  private int _version;
  [NonSerialized]
  private Object _syncRoot; 

  public Ring() : this(32) { } 

  public Ring(int capacity) { 
   if (capacity <= 0) {
//    throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum")); 
    throw new ArgumentOutOfRangeException("capacity", "Positive number required."); 
   }
   _array = new Object[capacity]; 
   _head = 0;
   _tail = 0;
   _size = 0;
  } 

  // Fills a Ring with the elements of an ICollection.  Uses the enumerator
  // to get each of the elements.
  // 
  public Ring(ICollection col) : this((col==null ? 32 : col.Count)) {
   if (col==null) {
    throw new ArgumentNullException("col");
   }
   IEnumerator en = col.GetEnumerator(); 
   while(en.MoveNext()) {
    Enqueue(en.Current);
   }
  }


  public virtual Object this[int index] {
   get {
    int i = (_head + index % _size) % _array.Length;
//    int i = (_head + index);
    return _array[i];
   }
   set {
    int i = (_head + index % _size) % _array.Length;
    _array[i] = value;
   }
  }

  public virtual int Count { 
   get { return _size; } 
  }

  public virtual Object Clone() {
   Ring r = new Ring(_size);
   r._size = _size;
  
   int numToCopy = _size;
   int firstPart = (_array.Length - _head < numToCopy) ? _array.Length - _head : numToCopy;
   Array.Copy(_array, _head, r._array, 0, firstPart);
   numToCopy -= firstPart;
   if (numToCopy > 0) {
    Array.Copy(_array, 0, r._array, _array.Length - _head, numToCopy);
   }

   r._version = _version;
   return r;
  }

  public virtual bool IsSynchronized { 
   get { return false; }
  } 

  public virtual Object SyncRoot {
   get {
    if( _syncRoot == null) { 
     System.Threading.Interlocked.CompareExchange(ref _syncRoot, new Object(), null);
    } 
    return _syncRoot; 
   }
  } 

  // Removes all Objects from the queue.
  public virtual void Clear() {
   if (_head < _tail) 
    Array.Clear(_array, _head, _size);
   else { 
    Array.Clear(_array, _head, _array.Length - _head); 
    Array.Clear(_array, 0, _tail);
   } 
 
   _head = 0;
   _tail = 0;
   _size = 0; 
   _version++;
  }


  public virtual void CopyTo(Array array, int index) {
   if (array==null) {
    throw new ArgumentNullException("array");
   }
   if (array.Rank != 1) {
//    throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
    throw new ArgumentException("Only single dimensional arrays are supported for the requested action.");
   }
   if (index < 0) {
//    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
    throw new ArgumentOutOfRangeException("index", "Index was out of range. Must be non-negative and less than the size of the collection.");
   }

   int arrayLen = array.Length;
   if (arrayLen - index < _size) {
//    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
    throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
   }
  
   int numToCopy = _size;
   if (numToCopy == 0) {
    return;
   }
   int firstPart = (_array.Length - _head < numToCopy) ? _array.Length - _head : numToCopy;
   Array.Copy(_array, _head, array, index, firstPart); 
   numToCopy -= firstPart;
   if (numToCopy > 0) {
    Array.Copy(_array, 0, array, index+_array.Length - _head, numToCopy);
   }
  } 

  // Adds obj to the tail of the queue. 
  // 
  public virtual void Enqueue(Object obj) {
   _array[_tail] = obj;
   _tail = (_tail + 1) % _array.Length;
   if (++_size > _array.Length) {
    _head = (_head + 1) % _array.Length;
    _size = _array.Length;
   }
   _version++;
  }

  public virtual IEnumerator GetEnumerator() {
   return new RingEnumerator(this);
  }

  // Removes the object at the head of the queue and returns it. If the queue 
  // is empty, this method simply returns null.
  public virtual Object Dequeue() {
   if (_size == 0) {
//    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EmptyQueue"));
    throw new InvalidOperationException("Ring empty.");
   }

   Object removed = _array[_head];
   _array[_head] = null;
   _head = (_head + 1) % _array.Length;
   _size--; 
   _version++;
   return removed; 
  }

  // Returns the object at the head of the queue. The object remains in the 
  // queue. If the queue is empty, this method throws an
  // InvalidOperationException.
  public virtual Object Peek() {
   if (_size == 0) {
//    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EmptyQueue"));
    throw new InvalidOperationException("Ring empty.");
   }

   return _array[_head]; 
  }

  [HostProtection(Synchronization=true)]
  public static Ring Synchronized(Ring ring) {
   if (ring==null) {
    throw new ArgumentNullException("ring");
   }
   return new SynchronizedRing(ring);
  }

  // Exceptions: ArgumentNullException if obj == null. 
  public virtual bool Contains(Object obj) {
   int index = _head;
   int count = _size;
 
   while (count-- > 0) {
    if (obj == null) { 
     if (_array[index] == null) {
      return true;
     }
    }
    else if (_array[index] != null && _array[index].Equals(obj)) {
     return true;
    }
    index = (index + 1) % _array.Length;
   }
 
   return false; 
  }
  
  internal Object GetElement(int i) {
   return _array[(_head + i) % _array.Length]; 
  }

  public virtual Object[] ToArray() {
   Object[] arr = new Object[_size];
   if (_size==0) {
    return arr;
   }

   if (_head < _tail) {
    Array.Copy(_array, _head, arr, 0, _size);
   }
   else {
    Array.Copy(_array, _head, arr, 0, _array.Length - _head);
    Array.Copy(_array, 0, arr, _array.Length - _head, _tail);
   }

   return arr;
  }


  public virtual void SetCapacity(int capacity) {
   if (capacity < _size || capacity <= 0) {
    throw new ArgumentOutOfRangeException("capacity", "Capacity was out of range. Must be positive and not less than the size of the collection.");
   }
   Object[] newarray = new Object[capacity]; 
   if (_size > 0) {
    if (_head < _tail) {
     Array.Copy(_array, _head, newarray, 0, _size);
    }
    else { 
     Array.Copy(_array, _head, newarray, 0, _array.Length - _head);
     Array.Copy(_array, 0, newarray, _array.Length - _head, _tail); 
    } 
   }

   _array = newarray;
   _head = 0;
   _tail = (_size == capacity) ? 0 : _size;
   _version++; 
  }

  public virtual void TrimToSize() {
   if (_size <= 0) {
    throw new InvalidOperationException("Ring empty. Cannot trim to zero capacity.");
   }
   SetCapacity(_size);
  }


  [Serializable()] private class SynchronizedRing : Ring { 
   private Ring _r;
   private Object root;
  
   internal SynchronizedRing(Ring r) {
    this._r = r;
    root = _r.SyncRoot;
   } 
 
   public override bool IsSynchronized { 
    get { return true; } 
   }
  
   public override Object SyncRoot {
    get {
     return root;
    } 
   }

   public override Object this[int index] {
    get {
     lock (root) { 
      return _r[index];
     }
    }
    set {
     lock (root) { 
      _r[index] = value;
     }
    }
   }

   public override int Count { 
    get {
     lock (root) { 
      return _r.Count;
     }
    }
   } 
 
   public override void Clear() { 
    lock (root) { 
     _r.Clear();
    } 
   }
 
   public override Object Clone() {
    lock (root) { 
     return new SynchronizedRing((Ring)_r.Clone());
    } 
   } 
 
   public override bool Contains(Object obj) { 
    lock (root) {
     return _r.Contains(obj);
    }
   } 
 
   public override void CopyTo(Array array, int arrayIndex) { 
    lock (root) { 
     _r.CopyTo(array, arrayIndex);
    } 
   }
 
   public override void Enqueue(Object value) {
    lock (root) { 
     _r.Enqueue(value);
    } 
   } 
 
   public override Object Dequeue() { 
    lock (root) {
     return _r.Dequeue();
    }
   } 
 
   public override IEnumerator GetEnumerator() { 
    lock (root) { 
     return _r.GetEnumerator();
    } 
   }
 
   public override Object Peek() {
    lock (root) { 
     return _r.Peek();
    } 
   } 
 
   public override Object[] ToArray() { 
    lock (root) {
     return _r.ToArray();
    }
   } 

   public override void SetCapacity(int capacity) {
    lock (root) {
     _r.SetCapacity(capacity);
    } 
   }

   public override void TrimToSize() {
    lock (root) {
     _r.TrimToSize();
    } 
   }
  }
 
  
  // Implements an enumerator for a Ring.  The enumerator uses the
  // internal version number of the list to ensure that no modifications are 
  // made to the list while an enumeration is in progress. 
  [Serializable()] private class RingEnumerator : IEnumerator, ICloneable
  { 
   private Ring _r;
   private int _index;
   private int _version;
   private Object currentElement; 
 
   internal RingEnumerator(Ring r) { 
    _r = r; 
    _version = _r._version;
    _index = 0; 
    currentElement = _r._array;
    if (_r._size == 0)
     _index = -1;
   } 
 
   public Object Clone() 
   { 
    return MemberwiseClone();
   } 
 
   public virtual bool MoveNext() {
    if (_version != _r._version) {
//     throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
     throw new InvalidOperationException("ResId.InvalidOperation_EnumFailedVersion");
    }
    if (_index < 0) {
     currentElement = _r._array; 
     return false; 
    }
  
    currentElement = _r.GetElement(_index);
    _index++;
 
    if (_index == _r._size) 
     _index = -1;
    return true; 
   } 
 
   public virtual Object Current { 
    get {
     if (currentElement == _r._array) {
      if (_index == 0) {
//       throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
       throw new InvalidOperationException("ResId.InvalidOperation_EnumNotStarted");
      }
      else {
//       throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
       throw new InvalidOperationException("ResId.InvalidOperation_EnumEnded");
      }
     }
     return currentElement; 
    }
   }
 
   public virtual void Reset() { 
    if (_version != _r._version) {
//     throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
     throw new InvalidOperationException("ResId.InvalidOperation_EnumFailedVersion");
    }
    if (_r._size == 0) {
     _index = -1;
    }
    else {
     _index = 0;
    }
    currentElement = _r._array;
   }
  }
  
  internal class RingDebugView {
   private Ring ring; 
  
   public RingDebugView(Ring ring) {
    if(ring == null) {
     throw new ArgumentNullException("ring");
    }
 
    this.ring = ring;
   } 
 
   [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
   public Object[] Items { 
    get {
     return ring.ToArray(); 
    }
   }
  }
 } 
}

using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class UnityEventString : UnityEvent<string> { }

[Serializable]
public class UnityEventFloat : UnityEvent<float> { }

[Serializable]
public class UnityEventVector2 : UnityEvent<Vector2> { }

[Serializable]
public class UnityEventInt : UnityEvent<int> { }

[Serializable]
public class UnityEventBool : UnityEvent<bool> { }

[Serializable]
public class UnityEventTransform : UnityEvent<Transform> { }

[Serializable]
public class UnityEventGameObject : UnityEvent<GameObject> { }
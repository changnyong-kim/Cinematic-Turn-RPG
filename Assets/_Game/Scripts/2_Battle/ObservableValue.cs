using System;
using System.Collections.Generic;

public sealed class ObservableValue<T>
{
    private T _value;

    public T Value => _value;

    public event Action<T> OnValueChanged;

    public ObservableValue(T defaultValue = default)
    {
        _value = defaultValue;
    }

    public void SetValue(T value)
    {
        if (EqualityComparer<T>.Default.Equals(_value, value))
        {
            return;
        }

        _value = value;
        OnValueChanged?.Invoke(_value);
    }
}

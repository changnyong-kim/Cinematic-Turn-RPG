using System;
using System.Collections.Generic;

/// <summary>
/// 값 변경을 구독자에게 알리는 경량 Observable 클래스.
/// UI ViewModel에서 UniRx 없이 단순 데이터 바인딩을 구성하기 위해 사용한다.
/// </summary>
public sealed class ObservableValue<T>
{
    private T _value;

    public T Value => _value;

    public event Action<T> OnValueChanged;

    public ObservableValue(T defaultValue = default)
    {
        _value = defaultValue;
    }

    /// <summary>
    /// 값이 실제로 변경된 경우에만 저장하고 변경 이벤트를 발생시킨다.
    /// </summary>
    public void SetValue(T value)
    {
        if (EqualityComparer<T>.Default.Equals(_value, value))
        {
            return;
        }

        _value = value;
        NotifyValueChanged();
    }

    /// <summary>
    /// 현재 값을 강제로 다시 알린다.
    /// View가 새로 바인딩되었거나 초기 표시값을 다시 적용해야 할 때 사용한다.
    /// </summary>
    public void Refresh()
    {
        NotifyValueChanged();
    }

    private void NotifyValueChanged()
    {
        OnValueChanged?.Invoke(_value);
    }
}

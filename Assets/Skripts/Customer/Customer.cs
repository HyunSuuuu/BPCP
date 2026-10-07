namespace BurgerIdle.Customers
{
    /// <summary>
    /// 매장 손님. 줄 서기·음식 수령·퇴장은 QueueMember가 처리한다.
    /// (음식을 받은 뒤 테이블로 가서 식사하는 동작은 P7에서 OnServed를 재정의해 추가)
    /// </summary>
    public sealed class Customer : QueueMember
    {
    }
}

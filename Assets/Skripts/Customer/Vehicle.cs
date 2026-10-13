namespace BurgerIdle.Customers
{
    /// <summary>
    /// 드라이브스루 차량 손님. 줄 서기·수령·퇴장은 QueueMember 기본 동작을 그대로 쓴다.
    /// 포장 버거 1개를 받으면 바로 차량 출구로 퇴장한다. 테이블·팁·쓰레기 없음.
    /// 차량 프리팹 루트에 붙이고, CustomerSpawner의 Prefab에 연결한다 (출입문 종류 = Vehicle Entrance / Vehicle Exit).
    /// </summary>
    public sealed class Vehicle : QueueMember
    {
    }
}

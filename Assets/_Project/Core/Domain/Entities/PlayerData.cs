namespace Core.Domain.Entities
{
    public class PlayerData
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public int MovesLeft { get; set; } = 20;
    }
}
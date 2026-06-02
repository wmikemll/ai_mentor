namespace InstantBot.AdminPanel.Models;

public class DashboardViewModel
{
    public int Dau { get; set; }
    public int Mau { get; set; }
    public int TotalUsers { get; set; }
    public int PremiumUsers { get; set; }
    public int VipUsers { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal Mrr { get; set; }
    public decimal Arpu { get; set; }
    public decimal Ltv { get; set; }
    public List<DailyStats> Last30Days { get; set; } = [];
}

public record DailyStats(DateOnly Date, int NewUsers, int StarsSold);

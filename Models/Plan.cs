namespace PowerGym.Models;
public class Plan
{
    public int IdPlan { get; set; }
    public string Nombre { get; set; } = "";
    public int DuracionDias { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
}

namespace BlazorApp1.Data;

public class Knjiznica
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;

    public ICollection<KnjiznicaKnjige> KnjiznicaKnjige { get; set; } = new List<KnjiznicaKnjige>();
}

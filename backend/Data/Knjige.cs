namespace BlazorApp1.Data;

public class Knjige
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;

    public ICollection<KnjiznicaKnjige> KnjiznicaKnjige { get; set; } = new List<KnjiznicaKnjige>();
}

namespace BlazorApp1.Data;

public class KnjiznicaKnjige
{
    public int Id { get; set; }
    public int KnjiznicaId { get; set; }
    public int KnjigaId { get; set; }
    public int BrojPrimjeraka { get; set; }

    public Knjiznica Knjiznica { get; set; } = null!;
    public Knjige Knjiga { get; set; } = null!;
}

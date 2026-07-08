namespace SafeWoman.Domain.Entities;

/// <summary>
/// Archivo binario persistente en la BD. Se usa para foto DNI y evidencias
/// multimedia (imágenes, videos, PDFs).
///
/// Se guarda en la BD en lugar del filesystem del contenedor porque los PaaS
/// gratuitos (Render Free, Fly.io free, etc.) usan filesystem efímero — los
/// archivos se pierden al redeployar. PostgreSQL es persistente y sobrevive.
///
/// Trade-off: aumenta el tamaño de la BD (Neon free tier permite hasta 3 GB,
/// suficiente para ~1500 evidencias de 2 MB). Si crece más, se migraría a
/// almacenamiento externo (Cloudinary, S3, etc.).
/// </summary>
public class ArchivoAlmacenado
{
    public int      IdArchivo      { get; private set; }
    public byte[]   Contenido      { get; private set; } = default!;
    public string   ContentType    { get; private set; } = default!;
    public string   NombreOriginal { get; private set; } = default!;
    public long     Tamanio        { get; private set; }
    public string   Categoria      { get; private set; } = default!;  // "dni", "evidencias", "evidencias-anonimas"
    public DateTime FechaSubida    { get; private set; }

    private ArchivoAlmacenado() { }

    public static ArchivoAlmacenado Crear(
        byte[] contenido,
        string contentType,
        string nombreOriginal,
        string categoria)
    {
        return new ArchivoAlmacenado
        {
            Contenido      = contenido,
            ContentType    = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            NombreOriginal = nombreOriginal,
            Tamanio        = contenido.Length,
            Categoria      = categoria,
            FechaSubida    = DateTime.UtcNow
        };
    }
}

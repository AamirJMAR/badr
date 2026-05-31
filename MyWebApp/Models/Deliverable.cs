namespace MyWebApp.Models
{
    public class Deliverable
    {
        public int Id { get; set; }

        // 🟦 Nom du fichier affiché à l'utilisateur
        public string FileName { get; set; } = "";

        // 🟦 Chemin relatif du fichier stocké sura le serveur
        // Exemple : UploadedFiles/xxxx.pdf
        public string FilePath { get; set; } = "";

        // 🟦 Date d'upload
        public DateTime UploadDate { get; set; }

        // 🔗 Lien avec le projet
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
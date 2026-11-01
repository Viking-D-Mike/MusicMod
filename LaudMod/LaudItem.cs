using UnityEngine;

namespace LaudMod
{
    // El laud: con R entras y sales del modo tocar; cada tecla de nota suena una vez.
    // Toda la logica comun esta en InstrumentoItem.cs
    [DefaultExecutionOrder(1000)]
    public class LaudItem : InstrumentoItem
    {
        public override Instrumento Datos { get { return LaudPlugin.Laud; } }
    }
}

using System;

namespace eu.foodmission.platform
{
    [System.Serializable]
    public class AvatarPartConfig
    {
        public int idPart; // 0: sin parte, 1-10: partes disponibles
        public int idColor; // 1-10: colores disponibles

        public AvatarPartConfig Copy()
        {
            return new AvatarPartConfig
            {
                idPart = idPart,
                idColor = idColor
            };
        }
    }

    [System.Serializable]
    public class AvatarConfig
    {
        public AvatarPartConfig hair;
        public AvatarPartConfig eyebrows;
        public AvatarPartConfig eyes;
        public AvatarPartConfig nose;
        public AvatarPartConfig mouth;
        public AvatarPartConfig facialHair;
        public AvatarPartConfig skin;
        public AvatarPartConfig tshirt;
        public AvatarPartConfig trousers;
        public AvatarPartConfig shoes;

        public AvatarConfig Copy()
        {
            return new AvatarConfig
            {
                hair = hair?.Copy(),
                eyebrows = eyebrows?.Copy(),
                eyes = eyes?.Copy(),
                nose = nose?.Copy(),
                mouth = mouth?.Copy(),
                facialHair = facialHair?.Copy(),
                skin = skin?.Copy(),
                tshirt = tshirt?.Copy(),
                trousers = trousers?.Copy(),
                shoes = shoes?.Copy()
            };
        }
    }
}
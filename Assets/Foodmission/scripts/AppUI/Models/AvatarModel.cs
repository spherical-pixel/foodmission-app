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

        /// <summary>
        /// Creates a standard, deterministic default avatar configuration for users who have not customized an avatar.
        /// </summary>
        public static AvatarConfig CreateDefault()
        {
            return new AvatarConfig
            {
                hair = new AvatarPartConfig { idPart = 5, idColor = 5 },
                eyebrows = new AvatarPartConfig { idPart = 7, idColor = 1 },
                eyes = new AvatarPartConfig { idPart = 1, idColor = 1 },
                nose = new AvatarPartConfig { idPart = 1, idColor = 1 },
                mouth = new AvatarPartConfig { idPart = 1, idColor = 1 },
                facialHair = new AvatarPartConfig { idPart = 0, idColor = 1 },
                skin = new AvatarPartConfig { idPart = 1, idColor = 1 },
                tshirt = new AvatarPartConfig { idPart = 1, idColor = 5 },
                trousers = new AvatarPartConfig { idPart = 1, idColor = 3 },
                shoes = new AvatarPartConfig { idPart = 1, idColor = 8 }
            };
        }
    }
}
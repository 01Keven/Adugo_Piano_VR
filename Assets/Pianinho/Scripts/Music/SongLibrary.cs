using System.Collections.Generic;
using UnityEngine;

namespace Pianinho
{
    /// <summary>As músicas que aparecem para escolha. Inclui uma música de teste gerada em código.</summary>
    [CreateAssetMenu(menuName = "Pianinho/Song Library", fileName = "SongLibrary")]
    public class SongLibrary : ScriptableObject
    {
        public List<SongData> songs = new List<SongData>();

        [Tooltip("Acrescenta uma música de teste gerada em código, para testar sem arquivos de áudio.")]
        public bool includeTestSong = true;

        SongData m_TestSong;

        /// <summary>Todas as músicas, inclusive as que ainda não têm áudio.</summary>
        public IReadOnlyList<SongData> All
        {
            get
            {
                var list = new List<SongData>();
                foreach (var song in songs)
                    if (song != null)
                        list.Add(song);
                if (includeTestSong)
                {
                    if (m_TestSong == null)
                        m_TestSong = TestSongFactory.Create();
                    list.Add(m_TestSong);
                }
                return list;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Cripto.Simetric
{
    public interface IXifradorSimetric
    {
        /// <summary>
        /// Xifra un conjunt de bytes utilitzant una key secreta.
        /// </summary>
        /// <param name="raw_data">Els bytes originals del missatge.</param>
        /// <param name="key">La key simètrica compartida.</param>
        /// <returns>Un vector de bytes amb el contingut xifrat.</returns>
        byte[] Xifrar(byte[] raw_data, byte[] key);

        /// <summary>
        /// Desxifra un conjunt de bytes utilitzant la mateixa key secreta.
        /// </summary>
        /// <param name="dadesXifrades">Els bytes protegits rebuts.</param>
        /// <param name="key">La key simètrica compartida.</param>
        /// <returns>Un vector de bytes desxifrat amb el missatge original.</returns>
        byte[] Desxifrar(byte[] encrypted_data, byte[] key);
    }
}

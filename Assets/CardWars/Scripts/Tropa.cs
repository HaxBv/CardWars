using UnityEngine;
using System;

public class Tropa : MonoBehaviour
{
    private int ataqueBase;
    private int vidaMaxima;

    [Header("Estadísticas Actuales")]
    public int ataqueActual;
    public int vidaActual;
    public int armadura;
    public int escudo;
    public int armaduraDeEscudo;
    public int quemadura;
    public int ataqueDirecto;
    public int laneIndex;
    public bool EsAliada;

    public event Action OnVidaModificada;

    public int VidaMaxima => vidaMaxima;
    public int VidaActual => vidaActual;

    public void Inicializar(int atk, int hp, int arm, int shld, int shldArm, int burn, int dirAtk)
    {
        ataqueBase = atk;
        ataqueActual = atk;
        vidaMaxima = hp;
        vidaActual = hp;
        armadura = arm;
        escudo = shld;
        armaduraDeEscudo = shldArm;
        quemadura = burn;
        ataqueDirecto = dirAtk;
    }

    /// <block>
    /// Gestiona cómo la tropa mitiga y descuenta el daño recibido.
    /// </block>
    public void RecibirDaño(int cantidad)
    {
        if (cantidad <= 0) return;

        // 1. El daño impacta primero al Escudo (Shield) si tiene
        if (escudo > 0)
        {
            // Aquí puedes aplicar la lógica de 'armaduraDeEscudo' si reduce el daño del escudo
            if (cantidad <= escudo)
            {
                escudo -= cantidad;
                cantidad = 0;
            }
            else
            {
                cantidad -= escudo;
                escudo = 0;
            }
        }

        // 2. El daño restante impacta a la Armadura común
        if (cantidad > 0 && armadura > 0)
        {
            if (cantidad <= armadura)
            {
                armadura -= cantidad;
                cantidad = 0;
            }
            else
            {
                cantidad -= armadura;
                armadura = 0;
            }
        }

        // 3. El daño final va directo a la salud
        if (cantidad > 0)
        {
            vidaActual = Mathf.Clamp(vidaActual - cantidad, 0, vidaMaxima);

            // DISPARAR EVENTO: Avisa a las RuntimeAbilities independientes que la vida cambió
            OnVidaModificada?.Invoke();
        }

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    /// <block>
    /// Sana a la tropa sin sobrepasar su salud máxima.
    /// </block>
    public void Curar(int cantidad)
    {
        if (cantidad <= 0 || vidaActual >= vidaMaxima) return;

        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0, vidaMaxima);

        OnVidaModificada?.Invoke();
    }

    private void Morir()
    {
        Debug.Log($"{gameObject.name} ha muerto.");
        Destroy(gameObject);
    }
}
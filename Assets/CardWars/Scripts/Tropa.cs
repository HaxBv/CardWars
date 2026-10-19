
using System;
using System.Collections.Generic;
using UnityEngine;

public class Tropa : MonoBehaviour
{
    [Header("Datos de la Carta")]
    [SerializeField] private TroopCardData datosDeLaCarta;

    private int ataqueBase;
    private int vidaMaxima;
    private bool retirada;

    [Header("Estadísticas Actuales")]
    public int ataqueActual;
    public int vidaActual;
    public int Armor;
    public int escudo;
    public int armaduraDeEscudo;
    public int quemadura;
    public int ataqueDirecto;

    [Header("Posición en Tablero")]
    public int laneIndex;
    public bool EsAliada;

    // ==========================================
    // EVENTS
    // ==========================================

    public event Action<Tropa, StatType, int, int> OnStatChanged;
    public event Action<Tropa, int> OnDamaged;
    public event Action<Tropa> OnDeath;

    // ==========================================
    // PROPERTIES
    // ==========================================

    public int MaxHealth => vidaMaxima;
    public int CurrentHealth => vidaActual;
    public bool EstaRetirada => retirada;

    public TroopCardData DatosDeLaCarta => datosDeLaCarta;

    // ==========================================
    // INITIALIZATION
    // ==========================================

    public void InicializarDesdeCarta(TroopCardData carta)
    {
        if (carta == null)
        {
            Debug.LogError("TroopCardData is null.", this);
            return;
        }

        datosDeLaCarta = carta;

        Inicializar(
            carta.Attack,
            carta.Health,
            carta.Armor,
            carta.Shield,
            carta.ShieldArmor,
            carta.Burn,
            carta.Overstrike
        );

        // Inicializar pasivas propias de la carta
        activePassives.Clear();

        if (carta.AbilityPassives != null)
        {
            foreach (AbilityPassive passive in carta.AbilityPassives)
            {
                AddPassive(passive);
            }
        }
    }

    public void Inicializar(
        int atk,
        int hp,
        int arm,
        int shld,
        int shldArm,
        int burn,
        int dirAtk)
    {
        ataqueBase = atk;
        ataqueActual = atk;

        vidaMaxima = Mathf.Max(0, hp);
        vidaActual = vidaMaxima;

        Armor = Mathf.Max(0, arm);
        escudo = Mathf.Max(0, shld);
        armaduraDeEscudo = Mathf.Max(0, shldArm);

        quemadura = burn;
        ataqueDirecto = dirAtk;

        retirada = false;
    }
    // ==========================================
    // PASSIVE ABILITIES
    // ==========================================

    private readonly HashSet<AbilityPassive> activePassives = new();

    public bool HasPassive(AbilityPassive passive)
    {
        return activePassives.Contains(passive);
    }

    public void AddPassive(AbilityPassive passive)
    {
        if (passive == AbilityPassive.None)
            return;

        activePassives.Add(passive);
    }

    // ==========================================
    // STAT CHANGES
    // ==========================================

    private void NotificarCambio(
        StatType stat,
        int anterior,
        int actual)
    {
        if (anterior == actual)
            return;

        OnStatChanged?.Invoke(
            this,
            stat,
            anterior,
            actual
        );
    }

    public void ModificarAtaque(int cantidad)
    {
        if (retirada) return;

        int anterior = ataqueActual;

        ataqueActual = Mathf.Max(
            0,
            ataqueActual + cantidad
        );

        NotificarCambio(
            StatType.Attack,
            anterior,
            ataqueActual
        );
    }

    public void ModificarArmadura(int cantidad)
    {
        if (retirada) return;

        int anterior = Armor;

        Armor = Mathf.Max(
            0,
            Armor + cantidad
        );

        NotificarCambio(
            StatType.Armor,
            anterior,
            Armor
        );
    }

    public void ModificarEscudo(int cantidad)
    {
        if (retirada) return;

        int anterior = escudo;

        escudo = Mathf.Max(
            0,
            escudo + cantidad
        );

        NotificarCambio(
            StatType.Shield,
            anterior,
            escudo
        );
    }

    public void ModificarArmaduraEscudo(int cantidad)
    {
        if (retirada) return;

        int anterior = armaduraDeEscudo;

        armaduraDeEscudo = Mathf.Max(
            0,
            armaduraDeEscudo + cantidad
        );

        NotificarCambio(
            StatType.ShieldArmor,
            anterior,
            armaduraDeEscudo
        );
    }

    public void ModificarVidaMaxima(int cantidad)
    {
        if (retirada) return;

        int anterior = vidaActual;

        vidaMaxima = Mathf.Max(
            1,
            vidaMaxima + cantidad
        );

        vidaActual = Mathf.Clamp(
            vidaActual + cantidad,
            0,
            vidaMaxima
        );

        NotificarCambio(
            StatType.Health,
            anterior,
            vidaActual
        );

        if (vidaActual <= 0)
            Morir();
    }

    // ==========================================
    // DAMAGE
    // ==========================================

    public void RecibirDaño(int cantidad)
    {
        if (cantidad <= 0 || retirada || vidaActual <= 0)
            return;

        // El escudo absorbe el golpe completo.
        // El sobrante nunca pasa a HP.
        if (escudo > 0)
        {
            int dañoEscudo = Mathf.Max(
                0,
                cantidad - armaduraDeEscudo
            );

            ModificarEscudo(-dañoEscudo);
            return;
        }

        // ARMOR reduce el daño, pero no se consume.
        int dañoVida = Mathf.Max(
            0,
            cantidad - Armor
        );

        if (dañoVida <= 0)
            return;

        int anterior = vidaActual;

        vidaActual = Mathf.Max(
            0,
            vidaActual - dañoVida
        );

        int dañoReal = anterior - vidaActual;

        NotificarCambio(
            StatType.Health,
            anterior,
            vidaActual
        );

        if (dañoReal > 0)
        {
            OnDamaged?.Invoke(this, dañoReal);
        }

        if (vidaActual <= 0)
            Morir();
    }

    // ==========================================
    // HEALING
    // ==========================================

    public void Curar(int cantidad)
    {
        if (cantidad <= 0 || retirada || vidaActual <= 0)
            return;

        int anterior = vidaActual;

        vidaActual = Mathf.Min(
            vidaMaxima,
            vidaActual + cantidad
        );

        NotificarCambio(
            StatType.Health,
            anterior,
            vidaActual
        );
    }

    // ==========================================
    // ATTACK
    // ==========================================

    public bool PuedeAtacar()
    {
        return !retirada &&
               vidaActual > 0 &&
               ataqueActual > 0;
    }

    public void AtacarA(Tropa objetivo)
    {
        if (!PuedeAtacar() || objetivo == null)
            return;

        objetivo.RecibirDaño(ataqueActual);

        Debug.Log(
            $"{gameObject.name} ataca a " +
            $"{objetivo.gameObject.name} " +
            $"con {ataqueActual} ATK."
        );
    }

    // ==========================================
    // DEATH / EXECUTE
    // ==========================================

    public void Morir()
    {
        if (retirada) return;

        retirada = true;

        Debug.Log($"{gameObject.name} ha muerto.");

        // Solo Death activa este evento.
        OnDeath?.Invoke(this);

        RetirarDelTablero();
    }

    public void Ejecutar()
    {
        if (retirada) return;

        retirada = true;

        Debug.Log($"{gameObject.name} fue ejecutada.");

        // Execute no activa OnDeath.
        RetirarDelTablero();
    }

    private void RetirarDelTablero()
    {
        enabled = false;
        Destroy(gameObject);
    }
}

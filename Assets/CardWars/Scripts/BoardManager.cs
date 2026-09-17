using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private static BoardManager instancia;

    public static BoardManager Instance
    {
        get
        {
            if (instancia == null)
            {
                instancia = FindFirstObjectByType<BoardManager>();
            }

            if (instancia == null)
            {
                var objeto = new GameObject("BoardManager");
                instancia = objeto.AddComponent<BoardManager>();
            }

            return instancia;
        }
    }

    // Listas globales para rastrear las tropas activas en la escena
    [SerializeField] private List<TropaEnTablero> tropasAliadas = new List<TropaEnTablero>();
    [SerializeField] private List<TropaEnTablero> tropasEnemigas = new List<TropaEnTablero>();

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
        }
        else if (instancia != this)
        {
            Destroy(gameObject);
        }
    }

    /// <block>
    /// Registra una tropa cuando entra al juego. 
    /// Este método debes llamarlo desde 'TropaEnTablero' en su Start o AlEntrarAlTablero.
    /// </block>
    public void RegistrarTropa(TropaEnTablero tropa, bool esAliada)
    {
        if (esAliada)
        {
            if (!tropasAliadas.Contains(tropa)) tropasAliadas.Add(tropa);
        }
        else
        {
            if (!tropasEnemigas.Contains(tropa)) tropasEnemigas.Add(tropa);
        }
    }

    /// <block>
    /// Remueve la tropa de las listas cuando muere o regresa a la mano.
    /// </block>
    public void QuitarTropa(TropaEnTablero tropa)
    {
        if (tropasAliadas.Contains(tropa)) tropasAliadas.Remove(tropa);
        if (tropasEnemigas.Contains(tropa)) tropasEnemigas.Remove(tropa);
    }

    /// <block>
    /// LÓGICA REQUERIDA POR KONQUEST: 
    /// Cuenta cuántos enemigos válidos hay en el mismo carril considerando un radio/rango.
    /// </block>
    public int ContarEnemigosEnCarril(Tropa poseedorStats, int rango)
    {
        // 1. Conseguimos el componente principal de la tropa que pregunta
        TropaEnTablero dueño = poseedorStats.GetComponent<TropaEnTablero>();
        if (dueño == null) return 0;

        int carrilOrigen = dueño.laneIndex;
        int contador = 0;

        // 2. Determinamos cuál es la lista de enemigos de esta carta
        // Si el dueño es aliado, busca en la lista de enemigos. Si es enemigo, busca en aliados.
        List<TropaEnTablero> listaEnemigos = tropasAliadas.Contains(dueño) ? tropasEnemigas : tropasAliadas;

        // 3. Evaluamos cada enemigo en la lista
        foreach (var enemigo in listaEnemigos)
        {
            // Calculamos la distancia de carriles en valor absoluto (por si el radio abarca carriles adyacentes)
            int distanciaCarriles = Mathf.Abs(enemigo.laneIndex - carrilOrigen);

            // Si está dentro del rango/radio (ej: rango 0 significa estrictamente el mismo carril)
            if (distanciaCarriles <= rango)
            {
                contador++;
            }
        }

        Debug.Log($"TableroManager: Detectados {contador} enemigos en el carril {carrilOrigen} (Radio: {rango}) para {dueño.name}.");
        return contador;
    }
}
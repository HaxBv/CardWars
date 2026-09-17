# Sistema de Turnos

Fuente: `PhasesSystem.cs`.

Ronda en 5 pasos:
1. Tropas Jugador 1
2. Tropas Jugador 2
3. Hechizos y dominios Jugador 1
4. Hechizos y dominios Jugador 2
5. Combate automático

Eventos:
- `OnFinDeTurno` - se dispara al avanzar cualquier paso.
- `OnFaseCombateIniciada` - entra la fase de combate.
- `OnFinDelCombate` - termina el paso 5 y se inicia la siguiente ronda.
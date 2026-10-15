# Raios retos do simulador

Arquivo afetado: Assets/Scripts/SimulatorControllersFollowView.cs

- SimulatorControllersFollowView.current.cs.txt: versão aprovada com raios retos, origem alinhada ao controle, sem curvatura visual e controles com rotação fixa relativa à câmera.
- SimulatorControllersFollowView.previous.cs.txt: versão imediatamente anterior a esse ajuste; mantém os controles acompanhando a câmera, mas preserva a rotação do simulador e as configurações originais dos raios.

Para desfazer: fora do Play Mode, copie o conteúdo de previous.cs.txt para Assets/Scripts/SimulatorControllersFollowView.cs e aguarde a recompilação.
Para reaplicar: copie o conteúdo de current.cs.txt para o mesmo script.
Não substitua o arquivo .meta. As cópias têm extensão .txt para não serem compiladas pelo Unity.
Restaurar a cópia inteira também substitui eventuais alterações futuras nesse script; nesse caso, compare antes e reverta apenas o ajuste dos raios.
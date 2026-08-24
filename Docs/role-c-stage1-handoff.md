# Role C — Stage 1 handoff

## Contracts

- 위치: `Daeume.Core.MemoryPresentationChanged`, `Daeume.Core.MemoryCompleted`, `Daeume.Core.StringTable`, `Daeume.Core.AssistSettings`
- sample payload: `new MemoryCompleted("memory-stage01", "stage01-memory-revealed")`
- 발생 조건: `MemoryAnchor`의 마지막 문장을 진행하면 presentation을 닫고 `MemoryCompleted`를 발행한다.
- 소비 결과: `MemoryCompletionBridge`가 `StageOneChaseController.BeginChaseFromMemory()`를 호출해 B lane의 오염/추격 전환을 시작한다.

HUD는 A의 `PlayerHealthChanged`, `InteractionPromptChanged`를 소비한다. Pressure presentation은 B의 `ContaminationPressureChanged`를 소비하며 Stable/Echo/Intrusion을 0/0.35/1 강도로 매핑한다.

## Assets

- palette: `Assets/Data/Presentation/Stage01Palette.asset`
- core sprites: `Player_Core.asset`, `Terrain_Core.asset` (point-filtered 16 PPU placeholder)
- memory: `Assets/Prefabs/Memory/Stage01_MemoryAnchor.prefab`
- HUD/accessibility: `Assets/Prefabs/UI/Stage01_Presentation.prefab`
- audio/camera: `Assets/Prefabs/Presentation/Stage01_PressurePresentation.prefab`

`Stage01_Base`는 Role B 소유권을 보존하기 위해 수정하지 않았다. 통합 시 B 담당자가 Memory marker에 Memory prefab을 배치하고 UI/pressure prefab은 Persistent 또는 C 전용 additive presentation scene에서 로드한다.

## Verification

Unity MCP Play Mode focused smoke에서 StringTable lookup/fallback, Memory 상태 진입과 완료 payload, HP·prompt HUD event, pressure presentation, CameraShakeStrength 적용을 확인했다. 종료 후 Console error는 0건이다.

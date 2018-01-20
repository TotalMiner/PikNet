using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public enum CustomInternalPacket
{
    PlayerUpdate,
    SessionUpdate
}
public enum SessionUpdateType
{
    StateUpdate,
    SessionEnd
}
public enum PlayerUpdateType
{
    Join,
    Leave
}

public enum CustomTMDataPacket
{

}
public enum TotalMinerGamePacketType : byte
{
    None,
    EndOfPacket,
    ModPacket,
    ServerUncaughtExceptionReport,
    ServerData,
    ServerHighscoreData,
    ServerHighscoreDataRequest,
    ServerCaughtExceptionReport,
    ServerConfirmReceiptRequest,
    ServerReceiptConfirmed,
    ServerDataRequest,
    Command,
    GameData,
    GameDataRequest,
    GameProperties,
    GamePropertiesNonVital,
    GamePropertiesRequest,
    GlobalItemData,
    GameInstanceData,
    GameInstanceDataRequest,
    GameState,
    FileShare,
    FileShareAck,
    BlockChanges,
    BlockTextureChange,
    BlockTextureRemoved,
    DataBlockInfoRequest,
    DataBlockInfo,
    DataBlockChange,
    DataBlockRemove,
    ChunkDataRequest,
    UneditedChunkList,
    GeneratedChunkList,
    GeneratedChunksRequest,
    ChunkData,
    HeightData,
    OpenBlockRequest,
    OpenBlockConfirm,
    CloseBlock,
    PickupCreate,
    PickupRequest,
    PickupConfirm,
    KickGamer,
    RatingVote,
    WorldFavorited,
    LockedInfoRequest,
    LockedInfo,
    CustomDataRequest,
    CustomData,
    Permissions,
    CreativeCommand,
    Blast,
    Inventory,
    InventoryChanged,
    PlayerStatistics,
    PlayerStatisticsRequest,
    PlayerSkill,
    PlayerSkills,
    PlayerSkillsRequest,
    PlayerSettings,
    PlayerSettingsRequest,
    PlayerLoaded,
    Projectile,
    Heal,
    ActionLog,
    HistoryItem,
    HistoryTable,
    DamageDealt,
    KillConfirm,
    DoorChangeConfirm,
    TrapDoorChangeConfirm,
    PowerDeliver,
    PowerSignals,
    SignText,
    FloodAbort,
    Notification,
    MiniGame,
    MobInstanceData,
    MobSpawnData,
    MobSpawnDataRequest,
    Slider,
    Zone,
    CaveInStart,
    Weather,
    PriceChange,
    PriceList,
    PhotoThumbnailRequest,
    PhotoThumbnail,
    WifiTransmitterFrequency,
    BookIDRequest,
    BookIDConfirm,
    BookUpdate,
    ItemUnlock,
    SleepState,
    TopMapMarkerUpdate,
    TopMapMarkerRemove,
    ScriptExecute,
    ScriptEdited,
    ScriptDeleted,
    ScriptCancelled,
    ScriptInputResult,
    ScriptIntersectResult,
    AdventureScript,
    EventScript,
    ComponentAsTempRequest,
    ComponentAsTempRequestConfirm,
    ComponentAsTempRequestData,
    ComponentAsTemp,
    TextMessage,
    ArcadeState,
    zLastID
}
public enum NetworkSessionEndReason
{
    ClientSignedOut = 0,
    HostEndedSession = 1,
    RemovedByHost = 2,
    Disconnected = 3,
}
public enum NetworkSessionState
{
    Lobby = 0,
    Playing = 1,
    Ended = 2,
}
public enum NetworkSessionType
{
    Local = 0,
    SystemLink = 1,
    PlayerMatch = 2,
    Ranked = 3,
}
public enum GameMode
{
    None,
    DigDeep,
    Creative,
    Survival,
    Peaceful
}
public enum MapAttribute
{
    Exploration,
    Survival,
    Adventure,
    Construction,
    Arena,
    RPG,
    Challenge,
    Deathmatch,
    Component,
    Skilling,
    WorkInProgress,
    zLast,
    AvatarDesigner
}
public enum Master_Server_Op_In
{
    Connect
}
public enum Master_Server_Op_Out
{
    Connect
}
public enum Master_server_ConnectionType
{
    CreateSession,
    JoinSession,
    GetSessions
}
public enum YesNo
{
    Yes,
    No
}

public enum PacketType
{
    Internal,
    TMData
}
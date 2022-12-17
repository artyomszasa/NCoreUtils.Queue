using NCoreUtils.Queue.Proto;
using NCoreUtils.Proto;

namespace NCoreUtils;

[ProtoClient(typeof(MediaProcessingQueueInfo), typeof(MediaProcessingQueueSerializerContext))]
public partial class MediaProcessingQueueClient { }
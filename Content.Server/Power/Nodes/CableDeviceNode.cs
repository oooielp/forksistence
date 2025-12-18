using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Content.Server.MCTN.Components;
using Content.Server.MCTN.Systems;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes
{
    /// <summary>
    ///     Type of node that connects to a <see cref="CableNode"/> below it.
    /// </summary>
    [DataDefinition]
    [Virtual]
    public partial class CableDeviceNode : Node
    {
        /// <summary>
        /// If disabled, this cable device will never connect.
        /// </summary>
        /// <remarks>
        /// If you change this,
        /// you must manually call <see cref="NodeGroupSystem.QueueReflood"/> to update the node connections.
        /// </remarks>
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;

        public override bool Connectable(IEntityManager entMan, TransformComponent? xform = null)
        {
            if (!Enabled)
                return false;

            return base.Connectable(entMan, xform);
        }

        public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
            EntityQuery<NodeContainerComponent> nodeQuery,
            EntityQuery<TransformComponent> xformQuery,
            MapGridComponent? grid,
            IEntityManager entMan)
        {
            if (!xform.Anchored || grid == null)
                yield break;

            if (entMan.TryGetComponent<MCTNComponent>(Owner, out var mctn) && entMan.TrySystem<MCTNSystem>(out var uepSys))
            {
                var remoteNode = uepSys.GetRemoteConnectionFor(Owner, mctn, this);
                if (remoteNode != null)
                    yield return remoteNode;
            }

            var gridIndex = grid.TileIndicesFor(xform.Coordinates);

            foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, gridIndex))
            {
                if (node is CableNode)
                    yield return node;
            }
        }
    }
}

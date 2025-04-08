using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FluentBehaviourTree
{
    /// <summary>
    /// Runs child nodes in sequence, until one fails.
    /// </summary>
    public class SequenceNode : IParentBehaviourTreeNode
    {
        /// <summary>
        /// Name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// List of child nodes.
        /// </summary>
        private List<IBehaviourTreeNode> children = new List<IBehaviourTreeNode>(); //todo: this could be optimized as a baked array.

        /// <summary>
        /// Index of child being executed.
        /// </summary>
        private int index = 0;

        public SequenceNode(string name)
        {
            this.name = name;
        }

        public BehaviourTreeStatus Tick(TimeData time)
        {

            while (true)
            {
                var childStatus = children[index].Tick(time);
                
                switch (childStatus)
                {
                    case BehaviourTreeStatus.Success:
                        UnityEngine.Debug.Log(children[index].GetName() + "--> success");
                        index += 1;
                        if (index >= children.Count)
                        {
                            UnityEngine.Debug.Log( GetName() + " child count: " + children.Count + ", Index: " + index);
                            index = 0;
                            return BehaviourTreeStatus.Success;
                        }
                        Tick(time);
                        break;
                    case BehaviourTreeStatus.Failure:
                        UnityEngine.Debug.Log(children[index].GetName() + "--> failure, index: " + index);
                        index = 0;
                        return BehaviourTreeStatus.Failure;
                    case BehaviourTreeStatus.Running:
                        return BehaviourTreeStatus.Running;
                }
            }
        }

        /// <summary>
        /// Add a child to the sequence.
        /// </summary>
        public void AddChild(IBehaviourTreeNode child)
        {
            children.Add(child);
        }

        public string GetName()
        {
            return name;
        }

    }
}

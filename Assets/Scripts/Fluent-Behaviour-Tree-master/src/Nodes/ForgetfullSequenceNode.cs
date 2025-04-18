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
    public class ForgetfulSequenceNode : IParentBehaviourTreeNode
    {
        /// <summary>
        /// Name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// List of child nodes.
        /// </summary>
        private List<IBehaviourTreeNode> children = new List<IBehaviourTreeNode>(); //todo: this could be optimized as a baked array.

        public ForgetfulSequenceNode(string name)
        {
            this.name = name;
        }

        public BehaviourTreeStatus Tick(TimeData time)
        {

            int index = 0;

            while (true)
            {
                var childStatus = children[index].Tick(time);
                
                switch (childStatus)
                {
                    case BehaviourTreeStatus.Success:
                        index += 1;
                        if (index >= children.Count)
                        {
                            refresh();
                            return BehaviourTreeStatus.Success;
                        }
                        break;
                    case BehaviourTreeStatus.Failure:
                        refresh();
                        return BehaviourTreeStatus.Failure;
                    case BehaviourTreeStatus.Running:
                        return BehaviourTreeStatus.Running;
                }
            }
        }

        
        public BehaviourTreeStatus Tick(TimeData time, string parents)
        {
            parents = parents + " --> " + name;

            int index = 0;

            while (true)
            {
                var childStatus = children[index].Tick(time, parents);
                
                switch (childStatus)
                {
                    case BehaviourTreeStatus.Success:
                        index += 1;
                        if (index >= children.Count)
                        {
                            refresh();
                            return BehaviourTreeStatus.Success;
                        }
                        break;
                    case BehaviourTreeStatus.Failure:
                        refresh();
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

        public void refresh()
        {
            foreach (var child in children)
                child.refresh();
        }

    }
}

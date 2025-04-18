using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEditor.Animations;
using UnityEngine;

namespace FluentBehaviourTree
{
    /// <summary>
    /// Selects the first node that succeeds. Tries successive nodes until it finds one that doesn't fail.
    /// </summary>
    public class ForgetfulSelectorNode : IParentBehaviourTreeNode
    {
        /// <summary>
        /// The name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// List of child nodes.
        /// </summary>
        private List<IBehaviourTreeNode> children = new List<IBehaviourTreeNode>(); //todo: optimization, bake this to an array.

        public ForgetfulSelectorNode(string name)
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
                    case BehaviourTreeStatus.Failure:
                        index += 1;
                        if (index >= children.Count)
                        {
                            refresh();
                            return BehaviourTreeStatus.Failure;
                        }
                        break;
                    case BehaviourTreeStatus.Success:
                        refresh();
                        return BehaviourTreeStatus.Success;
                    case BehaviourTreeStatus.Running:
                        return BehaviourTreeStatus.Running;
                }

            }
        }

        public BehaviourTreeStatus Tick(TimeData time, string parents)
        {
            int index = 0;

            while (true)
            {
                var childStatus = children[index].Tick(time, parents + " --> " + name);
                
                switch (childStatus)
                {
                    case BehaviourTreeStatus.Failure:
                        index += 1;
                        if (index >= children.Count)
                        {
                            refresh();
                            return BehaviourTreeStatus.Failure;
                        }
                        break;
                    case BehaviourTreeStatus.Success:
                        refresh();
                        return BehaviourTreeStatus.Success;
                    case BehaviourTreeStatus.Running:
                        return BehaviourTreeStatus.Running;
                }

            }
        }

        /// <summary>
        /// Add a child node to the selector.
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

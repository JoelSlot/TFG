using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FluentBehaviourTree
{
    /// <summary>
    /// Selects the first node that succeeds. Tries successive nodes until it finds one that doesn't fail.
    /// </summary>
    public class SelectorNode : IParentBehaviourTreeNode
    {
        /// <summary>
        /// The name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// List of child nodes.
        /// </summary>
        private List<IBehaviourTreeNode> children = new List<IBehaviourTreeNode>(); //todo: optimization, bake this to an array.

        /// <summary>
        /// Index of child being executed.
        /// </summary>
        private int index = 0;


        public SelectorNode(string name)
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
                    case BehaviourTreeStatus.Failure:
                        UnityEngine.Debug.Log(children[index].GetName() + "--> failure");
                        index += 1;
                        if (index >= children.Count)
                        {
                            index = 0;
                            return BehaviourTreeStatus.Failure;
                        }
                        Tick(time);
                        break;
                    case BehaviourTreeStatus.Success:
                        UnityEngine.Debug.Log(children[index].GetName() + "--> success");
                        index = 0;
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

    }
}

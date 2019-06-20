﻿/*! 
@file AbstractRenderer.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epForceDirectedGraph.cs>
@date August 08, 2013
@brief Abstract Renderer Interface
@version 1.0

@section LICENSE

The MIT License (MIT)

Copyright (c) 2013 Woong Gyu La <juhgiyo@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

@section DESCRIPTION

An Interface for the Abstract Renderer Class.
*/

namespace EpForceDirectedGraph.cs
{
  public abstract class AbstractRenderer : IRenderer
  {
    protected IForceDirected forceDirected;

    public AbstractRenderer(IForceDirected iForceDirected)
    {
      forceDirected = iForceDirected;
    }

    public void Draw(float iTimeStep)
    {
      forceDirected.Calculate(iTimeStep);
      Clear();
      forceDirected.EachEdge(delegate (IEdge edge, Spring spring)
      {
        DrawEdge(edge, spring.Point1.Position, spring.Point2.Position);
      });
      forceDirected.EachNode(delegate (INode node, Point point)
      {
        DrawNode(node, point.Position);
      });
    }

    public abstract void Clear();
    protected abstract void DrawEdge(IEdge iEdge, AbstractVector iPosition1, AbstractVector iPosition2);
    protected abstract void DrawNode(INode iNode, AbstractVector iPosition);
  }
}

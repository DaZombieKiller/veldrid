﻿using System.Diagnostics;
using Veldrid.OpenGLBinding;
using static Veldrid.OpenGLBinding.OpenGLNative;
using static Veldrid.OpenGL.OpenGLUtil;

namespace Veldrid.OpenGL
{
    internal unsafe class OpenGLBuffer : DeviceBuffer, IOpenGLDeferredResource
    {
        public override uint SizeInBytes { get; }
        public override BufferUsage Usage { get; }

        public uint Buffer => buffer;

        public override bool IsDisposed => disposeRequested;

        public override string Name
        {
            get => name;
            set
            {
                name = value;
                nameChanged = true;
            }
        }

        public bool Created { get; private set; }
        private readonly OpenGLGraphicsDevice gd;
        private uint buffer;
        private readonly bool dynamic;
        private bool disposeRequested;

        private string name;
        private bool nameChanged;

        public OpenGLBuffer(OpenGLGraphicsDevice gd, uint sizeInBytes, BufferUsage usage)
        {
            this.gd = gd;
            SizeInBytes = sizeInBytes;
            dynamic = (usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;
            Usage = usage;
        }

        #region Disposal

        public override void Dispose()
        {
            if (!disposeRequested)
            {
                disposeRequested = true;
                gd.EnqueueDisposal(this);
            }
        }

        #endregion

        public void EnsureResourcesCreated()
        {
            if (!Created) CreateGLResources();

            if (nameChanged)
            {
                nameChanged = false;
                if (gd.Extensions.KhrDebug) SetObjectLabel(ObjectLabelIdentifier.Buffer, buffer, name);
            }
        }

        public void CreateGLResources()
        {
            Debug.Assert(!Created);

            if (gd.Extensions.ArbDirectStateAccess)
            {
                uint buffer;
                glCreateBuffers(1, &buffer);
                CheckLastError();
                this.buffer = buffer;

                glNamedBufferData(
                    this.buffer,
                    SizeInBytes,
                    null,
                    dynamic ? BufferUsageHint.DynamicDraw : BufferUsageHint.StaticDraw);
                CheckLastError();
            }
            else
            {
                glGenBuffers(1, out buffer);
                CheckLastError();

                glBindBuffer(BufferTarget.CopyReadBuffer, buffer);
                CheckLastError();

                glBufferData(
                    BufferTarget.CopyReadBuffer,
                    SizeInBytes,
                    null,
                    dynamic ? BufferUsageHint.DynamicDraw : BufferUsageHint.StaticDraw);
                CheckLastError();
            }

            Created = true;
        }

        public void DestroyGLResources()
        {
            uint buffer = this.buffer;
            glDeleteBuffers(1, ref buffer);
            CheckLastError();
        }
    }
}

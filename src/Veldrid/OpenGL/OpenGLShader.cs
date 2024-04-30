﻿using System.Text;
using Veldrid.OpenGLBinding;
using static Veldrid.OpenGLBinding.OpenGLNative;
using static Veldrid.OpenGL.OpenGLUtil;

namespace Veldrid.OpenGL
{
    internal unsafe class OpenGLShader : Shader, IOpenGLDeferredResource
    {
        public override bool IsDisposed => disposeRequested;

        public uint Shader { get; private set; }

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
        private readonly ShaderType shaderType;
        private readonly StagingBlock stagingBlock;

        private bool disposeRequested;
        private bool disposed;
        private string name;
        private bool nameChanged;

        public OpenGLShader(OpenGLGraphicsDevice gd, ShaderStages stage, StagingBlock stagingBlock, string entryPoint)
            : base(stage, entryPoint)
        {
#if VALIDATE_USAGE
            if (stage == ShaderStages.Compute && !gd.Extensions.ComputeShaders)
            {
                if (this.gd.BackendType == GraphicsBackend.OpenGLES)
                    throw new VeldridException("Compute shaders require OpenGL ES 3.1.");
                throw new VeldridException("Compute shaders require OpenGL 4.3 or ARB_compute_shader.");
            }
#endif
            this.gd = gd;
            shaderType = OpenGLFormats.VdToGLShaderType(stage);
            this.stagingBlock = stagingBlock;
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
            if (!Created) createGLResources();

            if (nameChanged)
            {
                nameChanged = false;
                if (gd.Extensions.KhrDebug) SetObjectLabel(ObjectLabelIdentifier.Shader, Shader, name);
            }
        }

        public void DestroyGLResources()
        {
            if (!disposed)
            {
                disposed = true;

                if (Created)
                {
                    glDeleteShader(Shader);
                    CheckLastError();
                }
                else
                    gd.StagingMemoryPool.Free(stagingBlock);
            }
        }

        private void createGLResources()
        {
            Shader = glCreateShader(shaderType);
            CheckLastError();

            byte* textPtr = (byte*)stagingBlock.Data;
            int length = (int)stagingBlock.SizeInBytes;
            byte** textsPtr = &textPtr;

            glShaderSource(Shader, 1, textsPtr, &length);
            CheckLastError();

            glCompileShader(Shader);
            CheckLastError();

            int compileStatus;
            glGetShaderiv(Shader, ShaderParameter.CompileStatus, &compileStatus);
            CheckLastError();

            if (compileStatus != 1)
            {
                int infoLogLength;
                glGetShaderiv(Shader, ShaderParameter.InfoLogLength, &infoLogLength);
                CheckLastError();

                byte* infoLog = stackalloc byte[infoLogLength];
                uint returnedInfoLength;
                glGetShaderInfoLog(Shader, (uint)infoLogLength, &returnedInfoLength, infoLog);
                CheckLastError();

                string message = infoLog != null
                    ? Encoding.UTF8.GetString(infoLog, (int)returnedInfoLength)
                    : "<null>";

                throw new VeldridException($"Unable to compile shader code for shader [{name}] of type {shaderType}: {message}");
            }

            gd.StagingMemoryPool.Free(stagingBlock);
            Created = true;
        }
    }
}

﻿using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrid.Vk.VulkanUtil;

namespace Veldrid.Vk
{
    internal unsafe class VkResourceLayout : ResourceLayout
    {
        public VkDescriptorSetLayout DescriptorSetLayout => _dsl;
        public VkDescriptorType[] DescriptorTypes { get; }

        public DescriptorResourceCounts DescriptorResourceCounts { get; }
        public new int DynamicBufferCount { get; }

        public override bool IsDisposed => _disposed;

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                _gd.SetResourceName(this, value);
            }
        }

        private readonly VkGraphicsDevice _gd;
        private readonly VkDescriptorSetLayout _dsl;
        private bool _disposed;
        private string _name;

        public VkResourceLayout(VkGraphicsDevice gd, ref ResourceLayoutDescription description)
            : base(ref description)
        {
            _gd = gd;
            var dslCI = VkDescriptorSetLayoutCreateInfo.New();
            var elements = description.Elements;
            DescriptorTypes = new VkDescriptorType[elements.Length];
            var bindings = stackalloc VkDescriptorSetLayoutBinding[elements.Length];

            uint uniformBufferCount = 0;
            uint uniformBufferDynamicCount = 0;
            uint sampledImageCount = 0;
            uint samplerCount = 0;
            uint storageBufferCount = 0;
            uint storageBufferDynamicCount = 0;
            uint storageImageCount = 0;

            for (uint i = 0; i < elements.Length; i++)
            {
                bindings[i].binding = i;
                bindings[i].descriptorCount = 1;
                var descriptorType = VkFormats.VdToVkDescriptorType(elements[i].Kind, elements[i].Options);
                bindings[i].descriptorType = descriptorType;
                bindings[i].stageFlags = VkFormats.VdToVkShaderStages(elements[i].Stages);
                if ((elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) != 0) DynamicBufferCount += 1;

                DescriptorTypes[i] = descriptorType;

                switch (descriptorType)
                {
                    case VkDescriptorType.Sampler:
                        samplerCount += 1;
                        break;

                    case VkDescriptorType.SampledImage:
                        sampledImageCount += 1;
                        break;

                    case VkDescriptorType.StorageImage:
                        storageImageCount += 1;
                        break;

                    case VkDescriptorType.UniformBuffer:
                        uniformBufferCount += 1;
                        break;

                    case VkDescriptorType.UniformBufferDynamic:
                        uniformBufferDynamicCount += 1;
                        break;

                    case VkDescriptorType.StorageBuffer:
                        storageBufferCount += 1;
                        break;

                    case VkDescriptorType.StorageBufferDynamic:
                        storageBufferDynamicCount += 1;
                        break;
                }
            }

            DescriptorResourceCounts = new DescriptorResourceCounts(
                uniformBufferCount,
                uniformBufferDynamicCount,
                sampledImageCount,
                samplerCount,
                storageBufferCount,
                storageBufferDynamicCount,
                storageImageCount);

            dslCI.bindingCount = (uint)elements.Length;
            dslCI.pBindings = bindings;

            var result = vkCreateDescriptorSetLayout(_gd.Device, ref dslCI, null, out _dsl);
            CheckResult(result);
        }

        #region Disposal

        public override void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                vkDestroyDescriptorSetLayout(_gd.Device, _dsl, null);
            }
        }

        #endregion
    }
}

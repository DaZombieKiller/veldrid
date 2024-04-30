namespace Veldrid.MTL
{
    internal class MtlResourceSet : ResourceSet
    {
        public new IBindableResource[] Resources { get; }
        public new MtlResourceLayout Layout { get; }

        public override bool IsDisposed => disposed;

        public override string Name { get; set; }
        private bool disposed;

        public MtlResourceSet(ref ResourceSetDescription description, MtlGraphicsDevice gd)
            : base(ref description)
        {
            Resources = Util.ShallowClone(description.BoundResources);
            Layout = Util.AssertSubtype<ResourceLayout, MtlResourceLayout>(description.Layout);
        }

        #region Disposal

        public override void Dispose()
        {
            disposed = true;
        }

        #endregion
    }
}

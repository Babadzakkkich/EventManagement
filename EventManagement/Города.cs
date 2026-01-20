namespace EventManagement
{
    using System;
    using System.Collections.Generic;
    
    public partial class Города
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Города()
        {
            this.Мероприятия = new HashSet<Мероприятия>();
        }
    
        public int Id { get; set; }
        public string Название { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Мероприятия> Мероприятия { get; set; }
    }
}

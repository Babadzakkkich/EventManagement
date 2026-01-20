namespace EventManagement
{
    using System;
    using System.Collections.Generic;
    
    public partial class Страны
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Страны()
        {
            this.Пользователи = new HashSet<Пользователи>();
        }
    
        public int Id { get; set; }
        public string НазваниеСтраны { get; set; }
        public string АнглийскоеНазвание { get; set; }
        public string Код { get; set; }
        public int Код2 { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Пользователи> Пользователи { get; set; }
    }
}
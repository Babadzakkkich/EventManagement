namespace EventManagement
{
    using System;
    using System.Collections.Generic;
    
    public partial class Активности
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Активности()
        {
            this.ЖюриАктивности = new HashSet<ЖюриАктивности>();
            this.УчастникиАктивностей = new HashSet<УчастникиАктивностей>();
        }
    
        public int Id { get; set; }
        public string Название { get; set; }
        public int МероприятиеId { get; set; }
        public int День { get; set; }
        public System.TimeSpan ВремяНачала { get; set; }
        public int МодераторId { get; set; }
    
        public virtual Мероприятия Мероприятия { get; set; }
        public virtual Пользователи Пользователи { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ЖюриАктивности> ЖюриАктивности { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<УчастникиАктивностей> УчастникиАктивностей { get; set; }
    }
}
namespace EventManagement
{
    using System;
    using System.Collections.Generic;
    
    public partial class Мероприятия
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Мероприятия()
        {
            this.Активности = new HashSet<Активности>();
        }
    
        public int Id { get; set; }
        public string Название { get; set; }
        public string Описание { get; set; }
        public System.DateTime ДатаНачала { get; set; }
        public int ДлительностьДней { get; set; }
        public int ГородId { get; set; }
        public int НаправлениеId { get; set; }
        public Nullable<int> ПобедительId { get; set; }
        public int ОрганизаторId { get; set; }
        public string Фото { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Активности> Активности { get; set; }
        public virtual Города Города { get; set; }
        public virtual Направления Направления { get; set; }
        public virtual Пользователи Пользователи { get; set; }
        public virtual Пользователи Пользователи1 { get; set; }
    }
}
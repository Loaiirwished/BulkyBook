﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db= db;
        }

        public void Update(OrderHeader obj)
        {
            _db.OrderHeaders.Update(obj);
        }

		public void UpdateStatus(int Id, string OrderStatus, string? PaymentStatus = null)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(x => x.Id == Id);
            if(orderFromDb != null)
            {
                orderFromDb.OrderStatus = OrderStatus;
                if(PaymentStatus != null)
                {
                    orderFromDb.PaymentStatus = PaymentStatus;
                }
            }
		}

		public void UpdateStripePaymentID(int Id, string sessionId, string PaymentIntentId)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(x=>x.Id== Id);
            orderFromDb.PaymentDate = DateTime.Now; 
            orderFromDb.SessionId = sessionId;
            orderFromDb.PaymentIntentId = PaymentIntentId;
		}
	}
}

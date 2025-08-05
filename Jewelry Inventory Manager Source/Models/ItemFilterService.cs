using System;
using System.Collections.Generic;
using System.Linq;
using Moonglow_DB.Data;

namespace Moonglow_DB.Models
{
    public class ItemFilterCriteria
    {
        public string SearchText { get; set; } = "";
        public int? CategoryId { get; set; }
        public bool ActiveOnly { get; set; } = true;
        public int? LocationId { get; set; } // For stock filtering
        public bool InStockOnly { get; set; } = false;
    }

    public class ItemFilterService
    {
        private readonly DatabaseContext _databaseContext;

        public ItemFilterService(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        public List<Product> FilterProducts(ItemFilterCriteria criteria, List<Product> allProducts = null)
        {
            var products = allProducts ?? _databaseContext.GetAllProducts();
            var filteredProducts = products.AsEnumerable();

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var searchText = criteria.SearchText.ToLower();
                filteredProducts = filteredProducts.Where(p => 
                    p.Name.ToLower().Contains(searchText) ||
                    p.SKU.ToLower().Contains(searchText) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchText))
                );
            }

            // Apply category filter
            if (criteria.CategoryId.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.CategoryId == criteria.CategoryId.Value);
            }

            // Apply active filter
            if (criteria.ActiveOnly)
            {
                filteredProducts = filteredProducts.Where(p => p.IsActive);
            }

            // Apply stock filter
            if (criteria.InStockOnly && criteria.LocationId.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => 
                    _databaseContext.GetProductStock(p.Id, criteria.LocationId.Value) > 0
                );
            }

            return filteredProducts.OrderBy(p => p.Name).ToList();
        }

        public List<Component> FilterComponents(ItemFilterCriteria criteria, List<Component> allComponents = null)
        {
            var components = allComponents ?? _databaseContext.GetAllComponents();
            var filteredComponents = components.AsEnumerable();

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var searchText = criteria.SearchText.ToLower();
                filteredComponents = filteredComponents.Where(c => 
                    c.Name.ToLower().Contains(searchText) ||
                    c.SKU.ToLower().Contains(searchText) ||
                    (c.Description != null && c.Description.ToLower().Contains(searchText))
                );
            }

            // Apply category filter
            if (criteria.CategoryId.HasValue)
            {
                filteredComponents = filteredComponents.Where(c => c.CategoryId == criteria.CategoryId.Value);
            }

            // Apply active filter
            if (criteria.ActiveOnly)
            {
                filteredComponents = filteredComponents.Where(c => c.IsActive);
            }

            // Apply stock filter
            if (criteria.InStockOnly && criteria.LocationId.HasValue)
            {
                filteredComponents = filteredComponents.Where(c => 
                    _databaseContext.GetComponentStock(c.Id, criteria.LocationId.Value) > 0
                );
            }

            return filteredComponents.OrderBy(c => c.Name).ToList();
        }

        public List<Category> GetActiveCategories()
        {
            return _databaseContext.GetAllCategories().Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
        }

        public List<Location> GetActiveLocations()
        {
            return _databaseContext.GetAllLocations().Where(l => l.IsActive).OrderBy(l => l.Name).ToList();
        }
    }
} 
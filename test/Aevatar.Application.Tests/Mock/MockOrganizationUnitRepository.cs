using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Specifications;

namespace Aevatar.Mock
{
    public class MockOrganizationUnitRepository : IRepository<OrganizationUnit, Guid>, ITransientDependency
    {
        private readonly List<OrganizationUnit> _organizationUnits = new List<OrganizationUnit>();

        public Task<OrganizationUnit> GetAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default)
        {
            var organizationUnit = _organizationUnits.FirstOrDefault(x => x.Id == id);
            if (organizationUnit == null)
            {
                organizationUnit = new OrganizationUnit(id, $"Test Organization {id}");
                _organizationUnits.Add(organizationUnit);
            }
            return Task.FromResult(organizationUnit);
        }

        public Task<OrganizationUnit> FindAsync(Expression<Func<OrganizationUnit, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
        {
            var organizationUnit = _organizationUnits.FirstOrDefault(predicate.Compile());
            return Task.FromResult(organizationUnit);
        }

        public Task<OrganizationUnit> InsertAsync(OrganizationUnit entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            _organizationUnits.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<OrganizationUnit> UpdateAsync(OrganizationUnit entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var index = _organizationUnits.FindIndex(x => x.Id == entity.Id);
            if (index >= 0)
            {
                _organizationUnits[index] = entity;
            }
            return Task.FromResult(entity);
        }

        public Task DeleteAsync(OrganizationUnit entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            _organizationUnits.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<List<OrganizationUnit>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_organizationUnits);
        }

        public Task<List<OrganizationUnit>> GetListAsync(Expression<Func<OrganizationUnit, bool>> predicate, bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_organizationUnits.Where(predicate.Compile()).ToList());
        }

        public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((long)_organizationUnits.Count);
        }

        public Task<long> GetCountAsync(Expression<Func<OrganizationUnit, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((long)_organizationUnits.Count(predicate.Compile()));
        }

        // 以下是实现IRepository接口所需的其他方法，但在当前测试场景中未使用
        public Task<IQueryable<OrganizationUnit>> GetQueryableAsync()
        {
            return Task.FromResult(_organizationUnits.AsQueryable());
        }

        public Task DeleteAsync(Expression<Func<OrganizationUnit, bool>> predicate, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var items = _organizationUnits.Where(predicate.Compile()).ToList();
            foreach (var item in items)
            {
                _organizationUnits.Remove(item);
            }
            return Task.CompletedTask;
        }

        public Task DeleteDirectAsync(Expression<Func<OrganizationUnit, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return DeleteAsync(predicate, false, cancellationToken);
        }

        public Task<OrganizationUnit> FindAsync(Expression<Func<OrganizationUnit, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return FindAsync(predicate, true, cancellationToken);
        }

        public IQueryable<OrganizationUnit> WithDetails()
        {
            return _organizationUnits.AsQueryable();
        }

        public IQueryable<OrganizationUnit> WithDetails(params Expression<Func<OrganizationUnit, object>>[] propertySelectors)
        {
            return _organizationUnits.AsQueryable();
        }
        
        public Task DeleteManyAsync(IEnumerable<Guid> ids, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            foreach (var id in ids)
            {
                var entity = _organizationUnits.FirstOrDefault(x => x.Id == id);
                if (entity != null)
                {
                    _organizationUnits.Remove(entity);
                }
            }
            return Task.CompletedTask;
        }

        public Task<List<OrganizationUnit>> GetListAsync(ISpecification<OrganizationUnit> specification, bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            var query = _organizationUnits.AsQueryable();
            query = specification.ApplyFilter(query);
            return Task.FromResult(query.ToList());
        }

        public Task<long> GetCountAsync(ISpecification<OrganizationUnit> specification, CancellationToken cancellationToken = default)
        {
            var query = _organizationUnits.AsQueryable();
            query = specification.ApplyFilter(query);
            return Task.FromResult((long)query.Count());
        }

        public Task<OrganizationUnit> GetAsync(Expression<Func<OrganizationUnit, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
        {
            var entity = _organizationUnits.FirstOrDefault(predicate.Compile());
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(OrganizationUnit));
            }
            return Task.FromResult(entity);
        }
    }
} 
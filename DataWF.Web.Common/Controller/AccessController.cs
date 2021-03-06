﻿using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [Auth]
    public class AccessController : ControllerBase
    {
        private DBTable GetTable(string name)
        {
            return DBService.Schems.DefaultSchema.Tables.FirstOrDefault(p => p.ItemType.Type.Name == name);
            //TypeHelper.ParseType(name);
            //if (type == null)
            //{
            //    return null;
            //}
            //return DBTable.GetTable(type);
        }

        public User CurrentUser => User.GetCommonUser();

        [HttpGet("Get/{name}")]
        public ActionResult<AccessValue> GetAccess([FromRoute]string name)
        {
            try
            {
                var table = GetTable(name);
                if (table == null)
                {
                    return NotFound();
                }
                return table.Access;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("GetProperty/{name}/{property}")]
        public ActionResult<AccessValue> GetPropertyAccess([FromRoute]string name, [FromRoute]string property)
        {
            try
            {
                var table = GetTable(name);
                var column = table?.ParseProperty(property);
                if (column == null)
                {
                    return NotFound();
                }
                return column.Access;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("GetItems/{name}/{id}")]
        public ActionResult<List<AccessItem>> GetAccessItems([FromRoute]string name, [FromRoute]string id)
        {
            var table = GetTable(name);
            if (table == null)
            {
                return NotFound();
            }
            var accessColumn = table.AccessKey;
            if (accessColumn == null)
            {
                return BadRequest($"Table {table} is not Accessable!");
            }
            try
            {
                var value = table.LoadItemById(id, DBLoadParam.Load | DBLoadParam.Referencing);
                if (value == null)
                {
                    return NotFound();
                }

                if (!accessColumn.Access.GetFlag(AccessType.Read, CurrentUser)
                    || !value.Access.GetFlag(AccessType.Read, CurrentUser))
                {
                    return Forbid();
                }

                return value.Access.Items;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPut("SetItems/{name}/{id}")]
        public async Task<ActionResult<bool>> SetAccessItems([FromRoute]string name, [FromRoute]string id, [FromBody]List<AccessItem> accessItems)
        {
            var table = GetTable(name);
            if (table == null)
            {
                return NotFound();
            }
            var accessColumn = table.AccessKey;
            if (accessColumn == null)
            {
                return BadRequest($"Table {table} is not Accessable!");
            }
            using (var transaction = new DBTransaction(table.Connection, CurrentUser))
            {
                try
                {
                    var value = table.LoadItemById(id, DBLoadParam.Load, null, transaction);
                    if (value == null)
                    {
                        return NotFound();
                    }
                    if (!accessColumn.Access.GetFlag(AccessType.Admin, CurrentUser)
                        && !value.Access.GetFlag(AccessType.Admin, CurrentUser)
                        && !table.Access.GetFlag(AccessType.Admin, CurrentUser))
                    {
                        return Forbid();
                    }
                    var buffer = value.Access?.Clone();
                    buffer.Add(accessItems);
                    value.Access = buffer;
                    await value.Save(transaction);
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Web.UI;

namespace Loudenvier.Utils.WebForms
{
    public static class ControlExtensions
    {
        /// <summary>
        /// Recursively enumerates all controls on a <see cref="ControlCollection"/> (<see cref="Control.Controls"/>)
        /// </summary>
        /// <remarks>This lovely method was taken from https://asp-blogs.azurewebsites.net/dfindley/linq-the-uber-findcontrol 
        /// by the amazing David Findley (https://asp-blogs.azurewebsites.net/dfindley)</remarks>
        /// <param name="controls">The collection of controls to enumerate</param>
        /// <returns>An enumerable which yields every control (and it's children) found in the control collection.</returns>
        public static IEnumerable<Control> All(this ControlCollection controls, params Type[] excludeChildrenFrom) {
            foreach (Control control in controls) {
                if (excludeChildrenFrom == null || control.GetType().NotIn(excludeChildrenFrom)) 
                    if (control.Controls.Count > 0) 
                        foreach (Control grandChild in control.Controls.All(excludeChildrenFrom))
                            yield return grandChild;

                yield return control;
            }
        }
    }
}

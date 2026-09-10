using Shouldly;
using Workvivo.Application.Features.Organization.Commands.SaveDepartment;
using Xunit;

namespace Workvivo.Application.Tests.Features.Organization;

public class SaveDepartmentCommandValidatorTests
{
    private readonly SaveDepartmentCommandValidator _validator = new();

    private static SaveDepartmentCommand Command(Guid? id = null, Guid? parentId = null) =>
        new(
            Id: id,
            OrganizationId: Guid.NewGuid(),
            ParentDepartmentId: parentId,
            Code: "ENG",
            NameAr: "الهندسة",
            NameEn: "Engineering",
            DescriptionAr: null,
            DescriptionEn: null,
            ManagerEmployeeId: null,
            SortOrder: 0,
            IsActive: true);

    [Fact]
    public void A_new_top_level_department_is_valid()
    {
        // The regression this pins. The self-parent rule compared ParentDepartmentId
        // against Id; with both null that is null != null, which is false, so the rule
        // reported a violation and creating a root department was impossible.
        _validator.Validate(Command()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_new_child_department_is_valid()
    {
        _validator.Validate(Command(parentId: Guid.NewGuid())).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void An_existing_department_moved_under_another_is_valid()
    {
        _validator.Validate(Command(id: Guid.NewGuid(), parentId: Guid.NewGuid())).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_department_cannot_be_its_own_parent()
    {
        var id = Guid.NewGuid();

        var result = _validator.Validate(Command(id: id, parentId: id));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(SaveDepartmentCommand.ParentDepartmentId));
    }

    [Fact]
    public void An_existing_department_may_be_moved_to_the_top_level()
    {
        _validator.Validate(Command(id: Guid.NewGuid())).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_code_is_required(string code)
    {
        var command = Command() with { Code = code };

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_arabic_name_is_required()
    {
        // Arabic is the required side across the schema; English is the optional one.
        _validator.Validate(Command() with { NameAr = "" }).IsValid.ShouldBeFalse();
        _validator.Validate(Command() with { NameEn = null }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void An_over_long_code_is_rejected_before_it_reaches_the_column()
    {
        _validator.Validate(Command() with { Code = new string('x', 200) }).IsValid.ShouldBeFalse();
    }
}

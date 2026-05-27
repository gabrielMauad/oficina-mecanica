using FluentValidation;

namespace OrdensServico.Application.Ordens.Commands.AprovarOrcamento;

public sealed class AprovarOrcamentoValidator : AbstractValidator<AprovarOrcamentoCommand>
{
    public AprovarOrcamentoValidator()
    {
        RuleFor(x => x.OrdemServicoId)
            .NotEmpty()
            .WithMessage("O ID da ordem de serviço não pode ser vazio.");
    }
}

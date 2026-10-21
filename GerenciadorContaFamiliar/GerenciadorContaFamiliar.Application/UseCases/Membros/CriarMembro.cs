using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;


namespace GerenciadorContaFamiliar.Application.UseCases.Membros;

public class CriarMembroUseCase : ICriarMembroUseCase
{
    private readonly IMembroRepository _membroRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICriarContaUseCase _criarContaUseCase;

    public CriarMembroUseCase(
        IMembroRepository membroRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICriarContaUseCase criarContaUseCase)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _criarContaUseCase = criarContaUseCase ?? throw new ArgumentNullException(nameof(criarContaUseCase));
    }

    public async Task<MembroResponse> ExecutarAsync(
        CriarMembroRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 0️⃣ Dupla checagem da senha (o [Compare] do request já barra na API; aqui vale para qualquer chamador)
        if (request.Senha != request.ConfirmacaoSenha)
            throw new ArgumentException("As senhas não coincidem.");

        // 1️⃣ Validação de duplicidade
        var existente = await _membroRepository.ObterPorNomeDeUsuarioAsync(request.NomeDeUsuario, cancellationToken);
        if (existente is not null)
            throw new ArgumentException("Já existe um membro com este nome de usuário.");

        existente = await _membroRepository.ObterPorEmailAsync(request.Email, cancellationToken);
        if (existente is not null)
            throw new ArgumentException("Já existe um membro com este email.");

        existente = await _membroRepository.ObterPorTelefoneAsync(request.Telefone, cancellationToken);
        if (existente is not null)
            throw new ArgumentException("Já existe um membro com este telefone.");

        // 2️⃣ Criar entidade de domínio
        var membro = new Membro(
            request.Nome,
            request.NomeDeUsuario,
            request.Email,
            request.Telefone,
            request.DataNascimento
        );
        // 3️⃣ Definir senha (hash)
        membro.DefinirSenha(request.Senha, _passwordHasher);

        // 4️⃣ Persistir via repositório — orquestração transacional
        // Este use case é o orquestrador da transação: cria o membro e delega a criação
        // da conta principal (com suas categorias padrão) ao CriarContaUseCase.
        // Os use cases internos NÃO abrem transação própria; apenas adicionam e salvam
        // dentro da transação explícita iniciada aqui (mesmo DbContext/IUnitOfWork escopado),
        // garantindo atomicidade: ou tudo é persistido, ou nada.
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _membroRepository.AdicionarAsync(membro, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken); // membro.Id gerado aqui

            await _criarContaUseCase.ExecutarAsync(new CriarContaRequest
            {
                Nome = "Conta Corrente",
                MembroId = membro.Id,
                ContaPrincipal = true
            }, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        // 5️⃣ Mapear para DTO de saída
        return new MembroResponse
        {
            Id = membro.Id,
            Nome = membro.Nome,
            NomeDeUsuario = membro.NomeDeUsuario,
            Email = membro.Email,
            Telefone = membro.Telefone,
            DataNascimento = membro.DataNascimento,
            Ativo = membro.Ativo,
            CriadoEm = membro.CriadoEm,
            ContaPrincipalId = membro.ContaPrincipalId
        };
    }
}

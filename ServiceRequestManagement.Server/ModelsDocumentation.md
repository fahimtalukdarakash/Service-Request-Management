## `Response` Class

The `Response` class encapsulates various responses returned from the server, including status details and collections of related data objects like `Registration`, `ServiceRequest`, `Message`, `ServiceRequestOffer`, `ServiceRequestOfferSelection`, and `Evaluation`.

### Properties

- **StatusCode**: The status code representing the result of the request (type: `int`).
- **StatusMessage**: A message providing additional information about the status (type: `string?`).
- **listRegistration**: A list of `Registration` objects (type: `List<Registration>?`).
- **Registration**: A single `Registration` object (type: `Registration?`).
- **listServiceRequests**: A list of `ServiceRequest` objects (type: `List<ServiceRequest>?`).
- **serviceRequest**: A single `ServiceRequest` object (type: `ServiceRequest?`).
- **listMessages**: A list of `Message` objects (type: `List<Message>?`).
- **listServiceRequestOffer**: A list of `ServiceRequestOffer` objects (type: `List<ServiceRequestOffer>?`).
- **serviceRequestOffer**: A single `ServiceRequestOffer` object (type: `ServiceRequestOffer?`).
- **listServiceRequestOrders**: A list of `ServiceRequestOfferSelection` objects (type: `List<ServiceRequestOfferSelection>?`).
- **serviceRequestOrder**: A single `ServiceRequestOfferSelection` object (type: `ServiceRequestOfferSelection?`).
- **listMessages2**: A list of `Message2` objects (type: `List<Message2>?`).
- **listEvaluations**: A list of `Evaluation` objects (type: `List<Evaluation>?`).

### Code

```csharp
public class Response
{
    public int StatusCode { get; set; }
    public string? StatusMessage { get; set; }
    public List<Registration>? listRegistration { get; set; }
    public Registration? Registration { get; set; }
    public List<ServiceRequest>? listServiceRequests { get; set; }
    public ServiceRequest? serviceRequest { get; set; }
    public List<Message>? listMessages { get; set; }
    public List<ServiceRequestOffer>? listServiceRequestOffer { get; set; }
    public ServiceRequestOffer? serviceRequestOffer { get; set; }
    public List<ServiceRequestOfferSelection>? listServiceRequestOrders { get; set; }
    public ServiceRequestOfferSelection? serviceRequestOrder { get; set; }
    public List<Message2>? listMessages2 { get; set; }
    public List<Evaluation>? listEvaluations { get; set; }
}

```

## `Registration` Class

The `Registration` class represents a user registration within the system. It captures the essential information related to a user's registration, including their personal details, role, and status.

### Properties

- **Id**: A unique identifier for the registration (type: `Guid`).
- **Name**: The name of the registered user (type: `string?`).
- **Email**: The email address of the user (type: `string?`).
- **Password**: The password associated with the user account (type: `string?`).
- **Role**: The role assigned to the user (type: `string?`).
- **IsActive**: Indicates whether the user account is active (type: `int`).
- **IsApproved**: Indicates whether the user account has been approved (type: `int`).

### Code

```csharp
public class Registration
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public int IsActive { get; set; }
    public int IsApproved { get; set; }
}

```

## `ProviderManager` Class

The `ProviderManager` class represents a provider manager in the system. It contains important information about the provider manager such as their name, department, email, password, and the timestamp of when they were created.

### Properties

- **ID**: A unique identifier for the provider manager, linked as a foreign key to the `Registration` table (type: `int`).
- **Name**: The name of the provider manager (type: `string?`).
- **Department**: The department to which the provider manager belongs (type: `string?`).
- **Email**: The email address of the provider manager (type: `string?`).
- **Password**: The password for the provider manager (type: `string?`).
- **CreatedAt**: The timestamp of when the provider manager was created in the system (type: `DateTime`).

### Code

```csharp
public class ProviderManager
{
    public int ID { get; set; } // Foreign Key to Registration table
    public string? Name { get; set; }
    public string? Department { get; set; } // Department of the provider manager
    public string? Email { get; set; } // Email of the provider manager
    public string? Password { get; set; } // Password of the provider manager
    public DateTime CreatedAt { get; set; } // Timestamp of creation
}

```

### `ServiceRequest` Class

This class represents a service request and holds general information about the request.

#### Properties

- **RequestID**: A unique identifier for the service request (type: `Guid`).
- **UserID**: The ID of the user who created the request (type: `Guid`).
- **MasterAgreementID**: The ID of the master agreement associated with the service request (type: `int`).
- **MasterAgreementName**: The name of the master agreement (type: `string?`).
- **TaskDescription**: A description of the task associated with the request (type: `string?`).
- **RequestType**: The type of the request (type: `string?`).
- **Project**: The project related to the service request (type: `string?`).
- **StartDate**: The start date of the service request (type: `DateTime?`).
- **EndDate**: The end date of the service request (type: `DateTime?`).
- **TotalManDays**: The total number of man-days estimated for the request (type: `int?`).
- **Location**: The location of the service request (type: `string?`).
- **ProviderManagerInfo**: The information about the provider manager (type: `string?`).
- **Consumer**: The consumer for the service request (type: `string?`).
- **Representatives**: The representatives for the service request (type: `string?`).
- **cycleStatus**: The cycle status of the service request (type: `string?`).
- **SelectedDomainName**: The selected domain name for the service request (type: `string?`).
- **DomainName**: The domain name associated with the request (type: `string?`).
- **DomainID**: The domain ID for the request (type: `int?`).
- **numberOfSpecialists**: The number of specialists required (type: `int?`).
- **numberOfOffers**: The number of offers for the request (type: `int?`).
- **IsApproved**: The approval status of the request (type: `int`).
- **RoleSpecific**: A list of `RoleSpecific` entries that specify the roles and related details for the request (type: `List<RoleSpecific>?`).

### `RoleSpecific` Class

This class holds role-specific data for a service request, including the domain, role, and technology level for the assigned users.

#### Properties

- **RoleID**: A unique identifier for the role (type: `Guid`).
- **RequestID**: The ID of the associated service request (type: `Guid`).
- **UserID**: The ID of the user who created the role-specific data (type: `Guid`).
- **DomainName**: The name of the domain (type: `string?`).
- **DomainId**: The ID of the domain (type: `int?`).
- **Role**: The role name (type: `string?`).
- **Level**: The level of the role (type: `string?`).
- **TechnologyLevel**: The technology level associated with the role (type: `string?`).
- **LocationType**: The type of location for the role (type: `string?`).
- **NumberOfEmployee**: The number of employees assigned to the role (type: `int`).

### `MasterAgreementDomain` Class

This class represents the domain information related to a master agreement.

#### Properties

- **DomainID**: The ID of the domain associated with the master agreement (type: `int`).
- **DomainName**: The name of the domain (type: `string?`).

### Code

```csharp
public class ServiceRequest
{
    public Guid RequestID { get; set; }
    public Guid UserID { get; set; }
    public int MasterAgreementID { get; set; }
    public string? MasterAgreementName { get; set; }
    public string? TaskDescription { get; set; }
    public string? RequestType { get; set; }
    public string? Project { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TotalManDays { get; set; }
    public string? Location { get; set; }
    public string? ProviderManagerInfo { get; set; }
    public string? Consumer { get; set; }
    public string? Representatives { get; set; }
    public string? cycleStatus { get; set; }
    public string? SelectedDomainName { get; set; }
    public string? DomainName { get; set; }
    public int? DomainID { get; set; }
    public int? numberOfSpecialists { get; set; }
    public int? numberOfOffers { get; set; }
    public int IsApproved { get; set; }
    public List<RoleSpecific>? RoleSpecific { get; set; }
}

public class RoleSpecific
{
    public Guid RoleID { get; set; } = Guid.NewGuid();
    public Guid RequestID { get; set; }
    public Guid UserID { get; set; }
    public string? DomainName { get; set; }
    public int? DomainId { get; set; }
    public string? Role { get; set; }
    public string? Level { get; set; }
    public string? TechnologyLevel { get; set; }
    public string? LocationType { get; set; }
    public int NumberOfEmployee { get; set; }
}

public class MasterAgreementDomain
{
    public int DomainID { get; set; }
    public string? DomainName { get; set; }
}
```

### `ServiceRequestOffer` Class

This class represents an offer associated with a service request.

#### Properties

- **ServiceRequestOfferId**: A unique identifier for the service request offer (type: `int`).
- **RequestID**: The ID of the associated service request (type: `Guid`).
- **UserID**: The ID of the user who created the service request offer (type: `Guid`).
- **MasterAgreementID**: The ID of the master agreement for the offer (type: `int`).
- **MasterAgreementName**: The name of the master agreement (type: `string?`).
- **TaskDescription**: The description of the task related to the offer (type: `string?`).
- **RequestType**: The type of request for the service offer (type: `string?`).
- **Project**: The name of the project related to the service offer (type: `string?`).
- **DomainID**: The ID of the domain for the service request offer (type: `int`).
- **DomainName**: The name of the domain for the service request offer (type: `string?`).
- **CycleStatus**: The status of the cycle for the service request offer (type: `string?`).
- **NumberOfSpecialists**: The number of specialists required for the service request (type: `int`).
- **NumberOfOffers**: The number of offers available for the request (type: `int`).
- **ServiceOffers**: A list of `ServiceOffer` entries associated with the service request offer (type: `List<ServiceOffer>?`).

### `ServiceOffer` Class

This class represents a service offer made by a provider for a specific service request offer.

#### Properties

- **OfferID**: A unique identifier for the service offer (type: `int`).
- **ServiceRequestOfferId**: The ID of the associated service request offer (type: `int`).
- **ProviderName**: The name of the provider making the offer (type: `string?`).
- **ProviderID**: The ID of the provider making the offer (type: `string?`).
- **EmployeeID**: The ID of the employee offering the service (type: `string?`).
- **Role**: The role for the employee offering the service (type: `string?`).
- **Level**: The level of the employee offering the service (type: `string?`).
- **TechnologyLevel**: The technology level of the offer (type: `string?`).
- **Price**: The price associated with the service offer (type: `decimal`).

### Code

```csharp
public class ServiceRequestOffer
{
    public int ServiceRequestOfferId { get; set; }
    public Guid RequestID { get; set; }
    public Guid UserID { get; set; }
    public int MasterAgreementID { get; set; }
    public string? MasterAgreementName { get; set; }
    public string? TaskDescription { get; set; }
    public string? RequestType { get; set; }
    public string? Project { get; set; }
    public int DomainID { get; set; }
    public string? DomainName { get; set; }
    public string? CycleStatus { get; set; }
    public int NumberOfSpecialists { get; set; }
    public int NumberOfOffers { get; set; }
    public List<ServiceOffer>? ServiceOffers { get; set; }
}

public class ServiceOffer
{
    public int OfferID { get; set; }
    public int ServiceRequestOfferId { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderID { get; set; }
    public string? EmployeeID { get; set; }
    public string? Role { get; set; }
    public string? Level { get; set; }
    public string? TechnologyLevel { get; set; }
    public decimal Price { get; set; }
}

```

### `ServiceOfferSelection` Class

This class represents the details of a specific service offer selection.

#### Properties

- **OfferID**: A unique identifier for the service offer (type: `int`).
- **ServiceRequestOfferId**: The ID of the associated service request offer (type: `int`).
- **ProviderName**: The name of the provider offering the service (type: `string?`).
- **ProviderID**: The ID of the provider (type: `string?`).
- **EmployeeID**: The ID of the employee handling the offer (type: `string?`).
- **Role**: The role of the employee (type: `string?`).
- **Level**: The level of the service or offer (type: `string?`).
- **TechnologyLevel**: The level of technology required for the offer (type: `string?`).
- **Price**: The price of the service offer (type: `decimal`).
- **Selection**: The selection status (type: `string?`).
- **Comment**: Any additional comments or details about the offer (type: `string?`).

### `ServiceRequestOfferSelection` Class

This class represents the service request offer selection, including the related service offers.

#### Properties

- **ServiceRequestOfferId**: The ID of the service request offer (type: `int`).
- **RequestID**: The unique ID of the service request (type: `Guid`).
- **UserID**: The ID of the user making the request (type: `Guid`).
- **MasterAgreementID**: The ID of the master agreement (type: `int`).
- **MasterAgreementName**: The name of the master agreement (type: `string?`).
- **TaskDescription**: A description of the task for the offer (type: `string?`).
- **RequestType**: The type of the request (type: `string?`).
- **Project**: The name of the project related to the request (type: `string?`).
- **DomainID**: The ID of the domain associated with the service request (type: `int`).
- **DomainName**: The name of the domain associated with the request (type: `string?`).
- **CycleStatus**: The current cycle status of the request (type: `string?`).
- **NumberOfSpecialists**: The number of specialists required for the service request (type: `int`).
- **NumberOfOffers**: The number of offers received for the service request (type: `int`).
- **ServiceOffers**: A list of `ServiceOfferSelection` objects representing the individual offers (type: `List<ServiceOfferSelection>?`).

### Code

```csharp
public class ServiceOfferSelection
{
    public int OfferID { get; set; }
    public int ServiceRequestOfferId { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderID { get; set; }
    public string? EmployeeID { get; set; }
    public string? Role { get; set; }
    public string? Level { get; set; }
    public string? TechnologyLevel { get; set; }
    public decimal Price { get; set; }
    public string? Selection { get; set; }
    public string? Comment { get; set; }
}

public class ServiceRequestOfferSelection
{
    public int ServiceRequestOfferId { get; set; }
    public Guid RequestID { get; set; }
    public Guid UserID { get; set; }
    public int MasterAgreementID { get; set; }
    public string? MasterAgreementName { get; set; }
    public string? TaskDescription { get; set; }
    public string? RequestType { get; set; }
    public string? Project { get; set; }
    public int DomainID { get; set; }
    public string? DomainName { get; set; }
    public string? CycleStatus { get; set; }
    public int NumberOfSpecialists { get; set; }
    public int NumberOfOffers { get; set; }
    public List<ServiceOfferSelection>? ServiceOffers { get; set; }
}

```

## `Evaluation` Class

The `Evaluation` class represents the evaluation details for a service request. This class captures important information about an agreement, its provider, and the associated scores.

### Properties

- **EvaluationID**: A unique identifier for the evaluation (type: `int`).
- **ServiceRequestId**: The ID of the associated service request (type: `Guid`).
- **AgreementID**: The ID of the agreement (type: `int`).
- **AgreementName**: The name of the agreement (type: `string?`).
- **TaskDescription**: A description of the task (type: `string?`).
- **Type**: The type of the evaluation (type: `string?`).
- **Project**: The name of the project (type: `string?`).
- **ProviderID**: The ID of the provider associated with the evaluation (type: `string?`).
- **ProviderName**: The name of the provider (type: `string?`).
- **TimelinessScore**: The score assigned for the timeliness of the provider (type: `int`).
- **QualityScore**: The score assigned for the quality of the service (type: `int`).
- **OverallScore**: The overall score, calculated based on timeliness and quality (type: `decimal`).
- **CreatedAt**: The timestamp when the evaluation was created (type: `DateTime`).

### Code

```csharp
public class Evaluation
{
    public int EvaluationID { get; set; }
    public Guid ServiceRequestId { get; set; }
    public int AgreementID { get; set; }
    public string? AgreementName { get; set; }
    public string? TaskDescription { get; set; }
    public string? Type { get; set; }
    public string? Project { get; set; }
    public string? ProviderID { get; set; }
    public string? ProviderName { get; set; }
    public int TimelinessScore { get; set; }
    public int QualityScore { get; set; }
    public decimal OverallScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

```

## `Message` Class

The `Message` class represents a message associated with a specific service request. It contains properties that capture details about the message, including sender information, the message content, and the timestamp.

### Properties

- **MessageID**: Unique identifier for the message (type: `Guid`).
- **ServiceRequestID**: The unique identifier for the associated service request (type: `Guid`).
- **SenderID**: The ID of the sender, which can be a user or a provider manager (type: `Guid`).
- **SenderRole**: The role of the sender, either 'User' or 'ProviderManager' (type: `string?`).
- **MessageContent**: The content of the message (type: `string?`).
- **Timestamp**: The date and time when the message was sent (type: `DateTime`).

### Code

```csharp
public class Message
{
    [Key]
    public Guid MessageID { get; set; } // Unique message ID

    [Required]
    public Guid ServiceRequestID { get; set; } // Links to a specific service request

    [Required]
    public Guid SenderID { get; set; } // ID of the sender (User or ProviderManager)

    [Required]
    [MaxLength(50)]
    public string? SenderRole { get; set; } // Role of the sender ('User' or 'ProviderManager')

    [Required]
    [MaxLength] // For NVARCHAR(MAX)
    public string? MessageContent { get; set; } // Message text

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.Now; // Time the message was sent
}

```

import React, { useState, useEffect, useContext } from "react";
import { useParams } from "react-router-dom";
import axios from "axios";
import { AuthContext } from "../AuthContext";

function UpdateServiceRequest() {
  const { requestID } = useParams();
  const { userRole, userID } = useContext(AuthContext);
  const [formData, setFormData] = useState({
    userID: "",
    taskDescription: "",
    requestType: "",
    project: "",
    startDate: "",
    endDate: "",
    totalManDays: "",
    numberOfSpecialists: "",
    numberOfOffers: "",
    location: "",
    providerManagerInfo: "",
    consumer: "",
    representatives: "",
    cycleStatus: "",
    masterAgreementID: "",
    masterAgreementName: "",
    selectedDomainName: "",
    roleSpecific: []
  });
  const [masterAgreements, setMasterAgreements] = useState([]);
  const [domains, setDomains] = useState([]);
  const [roles, setRoles] = useState([]);
  const [levels, setLevels] = useState([]);
  const [technologyLevels, setTechnologyLevels] = useState([]);
  const [message, setMessage] = useState("");

  useEffect(() => {
    const fetchServiceRequest = async () => {
      try {
        const response = await axios.get(
          "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetServiceRequestDetails",
          { params: { requestID } }
        );
  
        if (response.data.statusCode === 200) {
          const data = response.data.serviceRequest;
  
          setFormData({
            userID: data.userID || "",
            taskDescription: data.taskDescription || "",
            requestType: data.requestType || "",
            project: data.project || "",
            startDate: data.startDate ? data.startDate.split("T")[0] : "",
            endDate: data.endDate ? data.endDate.split("T")[0] : "",
            totalManDays: data.totalManDays || "",
            numberOfSpecialists: data.numberOfSpecialists || "",
            numberOfOffers: data.numberOfOffers || "",
            location: data.location || "",
            providerManagerInfo: data.providerManagerInfo || "",
            consumer: data.consumer || "",
            representatives: data.representatives || "",
            cycleStatus: data.cycleStatus || "",
            masterAgreementID: data.masterAgreementID || "",
            masterAgreementName: data.masterAgreementName || "",
            selectedDomainName: data.selectedDomainName || "",
            roleSpecific: data.roleSpecific || [],
          });
  
        } else {
          setMessage(response.data.statusMessage || "Failed to load data.");
        }
      } catch (error) {
        console.error("Error fetching service request:", error);
        setMessage("An error occurred while fetching service request.");
      }
    };

    const fetchMasterAgreements = async () => {
      try {
        const response = await axios.get(
          "https://agiledev-contractandprovidermana-production.up.railway.app/master-agreements/established-agreements/"
        );
        setMasterAgreements(response.data);
      } catch (error) {
        console.error("Error fetching master agreements:", error);
      }
    };

    fetchServiceRequest();
    fetchMasterAgreements();
  }, [requestID]);

  useEffect(() => {
    if (formData.masterAgreementID) {
      axios
        .get(
          `https://agiledev-contractandprovidermana-production.up.railway.app/master-agreements/established-agreements/${formData.masterAgreementID}`
        )
        .then((response) => {
          setDomains(response.data);
          // Populate the roles, levels, and technology levels based on the selected domain
          const selectedDomain = response.data.find(
            (d) => d.domainName === formData.selectedDomainName
          );
          if (selectedDomain) {
            setRoles([...new Set(selectedDomain.roleDetails.map((r) => r.role))]);
            setLevels([...new Set(selectedDomain.roleDetails.map((r) => r.level))]);
            setTechnologyLevels([...new Set(selectedDomain.roleDetails.map((r) => r.technologyLevel))]);
            
            // Set the existing role specific fields if available
            if (formData.roleSpecific && formData.roleSpecific.length > 0) {
              setFormData((prev) => ({
                ...prev,
                roleSpecific: formData.roleSpecific.map((role) => ({
                  ...role,
                  role: role.role || "",
                  level: role.level || "",
                  technologyLevel: role.technologyLevel || "",
                  locationType: role.locationType || "",
                  numberOfEmployee: role.numberOfEmployee || 1
                }))
              }));
            }
          }
        })
        .catch((error) => console.error("Error fetching domains:", error));
    }
  }, [formData.masterAgreementID, formData.selectedDomainName]);
  

  const handleDomainChange = (e) => {
    const domainName = e.target.value;
    setFormData((prev) => ({ ...prev, selectedDomainName: domainName }));
  
    const selectedDomain = domains.find((d) => d.domainName === domainName);
    if (selectedDomain) {
      setRoles([...new Set(selectedDomain.roleDetails.map((r) => r.role))]);
      setLevels([...new Set(selectedDomain.roleDetails.map((r) => r.level))]);
      setTechnologyLevels([...new Set(selectedDomain.roleDetails.map((r) => r.technologyLevel))]);
  
      // Update role-specific fields
      const updatedRoleSpecific = selectedDomain.roleDetails.map((r) => ({
        role: r.role,
        level: r.level,
        technologyLevel: r.technologyLevel,
        numberOfEmployee: r.numberOfEmployee || "",
        locationType: r.locationType || ""
      }));
  
      setFormData((prev) => ({ ...prev, roleSpecific: updatedRoleSpecific }));
    } else {
      setRoles([]);
      setLevels([]);
      setTechnologyLevels([]);
      setFormData((prev) => ({ ...prev, roleSpecific: [] }));
    }
  };
  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };
  const handleCycleChange = (e) => {
    setFormData({ ...formData, cycleStatus: e.target.value });
  };
  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      console.log("Submitting data:", JSON.stringify(formData, null, 2));
      
      const response = await axios.put(
        "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/UpdateServiceRequest",
        formData,
        { headers: { "Content-Type": "application/json" } }
      );
  
      console.log("API Response:", response.data);
  
      if (response.data.statusCode === 200) {
        setMessage("Service Request Updated Successfully!");
      } else {
        setMessage(`Failed to update the Service Request: ${response.data.statusMessage}`);
      }
    } catch (error) {
      console.error("Error updating service request:", error);
      setMessage(`An error occurred: ${error.response?.data?.message || error.message}`);
    }
  };  

  return (
    <div className="container mt-5">
      <h2>Update Service Request</h2>
      {message && <div className="alert alert-info">{message}</div>}
      <form onSubmit={handleSubmit}>
        <div className="mb-3">
          <label htmlFor="project" className="form-label">Project</label>
          <input
            type="text"
            id="project"
            name="project"
            className="form-control"
            value={formData.project}
            onChange={handleChange}
          />
        </div>
        <div className="mb-3">
          <label htmlFor="taskDescription" className="form-label">Task Description</label>
          <textarea
            id="taskDescription"
            name="taskDescription"
            className="form-control"
            value={formData.taskDescription}
            onChange={handleChange}
          ></textarea>
        </div>
        <div className="mb-3">
          <label htmlFor="startDate" className="form-label">Start Date</label>
          <input
            type="date"
            id="startDate"
            name="startDate"
            className="form-control"
            value={formData.startDate}
            onChange={handleChange}
          />
        </div>

        <div className="mb-3">
          <label htmlFor="endDate" className="form-label">End Date</label>
          <input
            type="date"
            id="endDate"
            name="endDate"
            className="form-control"
            value={formData.endDate}
            onChange={handleChange}
          />
        </div>
        <div className="mb-3">
          <label htmlFor="totalManDays" className="form-label">Total Man Days</label>
          <input
            type="number"
            id="totalManDays"
            name="totalManDays"
            className="form-control"
            value={formData.totalManDays}
            onChange={handleChange}
          />
        </div>

        <div className="mb-3">
          <label htmlFor="numberOfSpecialists" className="form-label">Number of Specialists</label>
          <input
            type="number"
            id="numberOfSpecialists"
            name="numberOfSpecialists"
            className="form-control"
            value={formData.numberOfSpecialists}
            onChange={handleChange}
          />
        </div>

        <div className="mb-3">
          <label htmlFor="numberOfOffers" className="form-label">Number of Offers</label>
          <input
            type="number"
            id="numberOfOffers"
            name="numberOfOffers"
            className="form-control"
            value={formData.numberOfOffers}
            onChange={handleChange}
          />
        </div>

        <div className="mb-3">
          <label htmlFor="location" className="form-label">Location</label>
          <input
            type="text"
            id="location"
            name="location"
            className="form-control"
            value={formData.location}
            onChange={handleChange}
          />
        </div>

        <div className="mb-3">
          <label htmlFor="providerManagerInfo" className="form-label">Provider Manager Info</label>
          <textarea
            id="providerManagerInfo"
            name="providerManagerInfo"
            className="form-control"
            value={formData.providerManagerInfo}
            onChange={handleChange}
          ></textarea>
        </div>

        <div className="mb-3">
          <label htmlFor="consumer" className="form-label">Consumer</label>
          <input
            type="text"
            id="consumer"
            name="consumer"
            className="form-control"
            value={formData.consumer}
            onChange={handleChange}
          />
        </div>

        <div className="mb-3">
          <label htmlFor="representatives" className="form-label">Representatives</label>
          <textarea
            id="representatives"
            name="representatives"
            className="form-control"
            value={formData.representatives}
            onChange={handleChange}
          ></textarea>
        </div>

        <div className="mb-3">
          <label htmlFor="masterAgreementID" className="form-label">Master Agreement</label>
          <select
            id="masterAgreementID"
            name="masterAgreementID"
            className="form-control"
            value={formData.masterAgreementID}
            onChange={handleChange}
          >
            <option value="">Select Master Agreement</option>
            {masterAgreements.map((ma) => (
              <option key={ma.agreementId} value={ma.agreementId}>{ma.name}</option>
            ))}
          </select>
        </div>
  
        <div className="mb-3">
          <label htmlFor="selectedDomainName" className="form-label">Domain</label>
          <select
            id="selectedDomainName"
            name="selectedDomainName"
            className="form-control"
            value={formData.selectedDomainName}
            onChange={handleDomainChange}
          >
            <option value="">Select Domain</option>
            {domains.map((domain) => (
              <option key={domain.domainName} value={domain.domainName}>{domain.domainName}</option>
            ))}
          </select>
        </div>
        <div className="mb-3">
          <label htmlFor="requestType" className="form-label">Request Type</label>
          <select
            id="requestType"
            name="requestType"
            className="form-control"
            value={formData.requestType}
            onChange={handleChange}
          >
            <option value="">Select Request Type</option>
            <option value="single">Single</option>
            <option value="multi">Multi</option>
            <option value="team">Team</option>
          </select>
        </div>
        <div className="mb-3">
          <label htmlFor="cycleStatus" className="form-label">Cycle</label>
          <select
            id="cycleStatus"
            name="cycleStatus"
            className="form-control"
            value={formData.cycleStatus}
            onChange={handleCycleChange}
          >
            <option value="">Select Cycle</option>
            <option value="1">Cycle 1</option>
            <option value="2">Cycle 2</option>
          </select>
        </div>
        {formData.roleSpecific && formData.roleSpecific.length > 0 && (
  <div>
    <h4>Role Specific Details</h4>
    {formData.roleSpecific.map((role, index) => (
      <div key={index} className="card bg-light p-3 mb-3">
        <div className="mb-3">
          <label htmlFor={`role-${index}`} className="form-label">Role</label>
          <select
            id={`role-${index}`}
            name={`role-${index}`}
            className="form-control"
            value={role.role}
            onChange={(e) => {
              const updatedRoles = [...formData.roleSpecific];
              updatedRoles[index].role = e.target.value;
              setFormData({ ...formData, roleSpecific: updatedRoles });
            }}
          >
            <option value="">Select Role</option>
            {roles.map((r, idx) => (
              <option key={idx} value={r}>{r}</option>
            ))}
          </select>
        </div>

        <div className="mb-3">
          <label htmlFor={`level-${index}`} className="form-label">Level</label>
          <select
            id={`level-${index}`}
            name={`level-${index}`}
            className="form-control"
            value={role.level}
            onChange={(e) => {
              const updatedRoles = [...formData.roleSpecific];
              updatedRoles[index].level = e.target.value;
              setFormData({ ...formData, roleSpecific: updatedRoles });
            }}
          >
            <option value="">Select Level</option>
            {levels.map((l, idx) => (
              <option key={idx} value={l}>{l}</option>
            ))}
          </select>
        </div>

        <div className="mb-3">
          <label htmlFor={`technologyLevel-${index}`} className="form-label">Technology Level</label>
          <select
            id={`technologyLevel-${index}`}
            name={`technologyLevel-${index}`}
            className="form-control"
            value={role.technologyLevel}
            onChange={(e) => {
              const updatedRoles = [...formData.roleSpecific];
              updatedRoles[index].technologyLevel = e.target.value;
              setFormData({ ...formData, roleSpecific: updatedRoles });
            }}
          >
            <option value="">Select Technology Level</option>
            {technologyLevels.map((t, idx) => (
              <option key={idx} value={t}>{t}</option>
            ))}
          </select>
        </div>

        <div className="mb-3">
          <label htmlFor={`locationType-${index}`} className="form-label">Location Type</label>
          <select
            id={`locationType-${index}`}
            name={`locationType-${index}`}
            className="form-control"
            value={role.locationType}
            onChange={(e) => {
              const updatedRoles = [...formData.roleSpecific];
              updatedRoles[index].locationType = e.target.value;
              setFormData({ ...formData, roleSpecific: updatedRoles });
            }}
          >
            <option value="">Select Location Type</option>
            <option value="onsite">Onsite</option>
            <option value="remote">Remote</option>
            <option value="hybrid">Hybrid</option>
          </select>
        </div>

        <div className="mb-3">
          <label htmlFor={`numberOfEmployee-${index}`} className="form-label">Number of Employees</label>
          <input
            type="number"
            id={`numberOfEmployee-${index}`}
            name={`numberOfEmployee-${index}`}
            className="form-control"
            value={role.numberOfEmployee}
            onChange={(e) => {
              const updatedRoles = [...formData.roleSpecific];
              updatedRoles[index].numberOfEmployee = e.target.value;
              setFormData({ ...formData, roleSpecific: updatedRoles });
            }}
            min={1}
          />
        </div>
      </div>
    ))}
  </div>
)}
  
        <button type="submit" className="btn btn-primary">Update</button>
      </form>
    </div>
  );  
}

export default UpdateServiceRequest;
